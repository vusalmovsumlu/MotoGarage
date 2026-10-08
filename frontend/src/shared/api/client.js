import { API_ROOT } from "../config/environment.js";

const SESSION_KEY = "motogarage.session";

let session = null;
try {
  session = JSON.parse(sessionStorage.getItem(SESSION_KEY));
} catch {
  /* Browser storage may be unavailable. */
}
let rotation = null;
let revision = 0;
const listeners = new Set();

export function getSession() {
  return session;
}
export function subscribeSession(listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
export function setSession(value) {
  session = value;
  revision++;
  try {
    if (value) sessionStorage.setItem(SESSION_KEY, JSON.stringify(value));
    else sessionStorage.removeItem(SESSION_KEY);
  } catch {
    /* In-memory session still works. */
  }
  listeners.forEach((listener) => listener());
}
export function claims(token) {
  try {
    const encoded = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(
      new TextDecoder().decode(
        Uint8Array.from(
          atob(encoded.padEnd(Math.ceil(encoded.length / 4) * 4, "=")),
          (x) => x.charCodeAt(0),
        ),
      ),
    );
  } catch {
    return {};
  }
}
export function sessionUser(value = session) {
  if (!value) return null;
  const token = claims(value.accessToken);
  return {
    id: Number(
      token[
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
      ] ||
        token.nameid ||
        token.sub,
    ),
    fullName:
      token["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
      token.unique_name ||
      "İstifadəçi",
    email:
      token[
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
      ] ||
      token.email ||
      "",
    role:
      token["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
      token.role,
  };
}
export class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}
async function readResponse(response) {
  if (response.status === 204) return null;
  const body = await response.text();
  let data;
  try {
    data = body ? JSON.parse(body) : null;
  } catch {
    data = null;
  }
  if (!response.ok) {
    const validation = data?.errors
      ? Object.values(data.errors).flat().join(" ")
      : "";
    throw new ApiError(
      validation ||
        data?.message ||
        (response.status === 429
          ? "Çox sayda sorğu göndərildi. Bir qədər gözləyib yenidən cəhd edin."
          : response.status === 401
            ? "Sessiya bitib. Yenidən daxil olun."
            : response.status === 403
              ? "Bu əməliyyat üçün icazəniz yoxdur."
              : "Sorğu tamamlanmadı. Yenidən cəhd edin."),
      response.status,
    );
  }
  return data;
}
export async function refreshSession() {
  if (rotation) return rotation;
  const current = session;
  if (!current?.refreshToken) throw new ApiError("Yenidən daxil olun.", 401);
  const version = revision;
  rotation = (async () => {
    try {
      const response = await fetch(`${API_ROOT}/auth/refresh-token`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: current.refreshToken }),
      });
      const next = await readResponse(response);
      if (version !== revision)
        throw new ApiError("Sessiya dəyişib. Yenidən cəhd edin.", 401);
      setSession(next);
      return next.accessToken;
    } catch (error) {
      if (version === revision && [400, 401, 403].includes(error.status))
        setSession(null);
      throw error;
    } finally {
      rotation = null;
    }
  })();
  return rotation;
}
export async function getAccessToken() {
  if (!session) throw new ApiError("Yenidən daxil olun.", 401);
  if ((claims(session.accessToken).exp || 0) * 1000 < Date.now() + 15000)
    return refreshSession();
  return session.accessToken;
}
export async function api(
  path,
  { method = "GET", body, signal, anonymous = false } = {},
) {
  let token = anonymous ? null : await getAccessToken();
  const send = () =>
    fetch(`${API_ROOT}${path}`, {
      method,
      signal,
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(body && !(body instanceof FormData)
          ? { "Content-Type": "application/json" }
          : {}),
      },
      body:
        body instanceof FormData
          ? body
          : body === undefined
            ? undefined
            : JSON.stringify(body),
    });
  try {
    let response = await send();
    if (response.status === 401 && !anonymous) {
      if (session?.accessToken === token) token = await refreshSession();
      else token = await getAccessToken();
      response = await send();
      if (response.status === 401) setSession(null);
    }
    return await readResponse(response);
  } catch (error) {
    if (error.name === "AbortError" || error instanceof ApiError) throw error;
    throw new ApiError(
      "Serverə qoşulmaq mümkün olmadı. İnternet və API bağlantısını yoxlayın.",
      0,
    );
  }
}
