import { api } from "../../../shared/api/client.js";

export const authApi = {
  registerBusiness: (values, photos) => {
    const body = new FormData();
    Object.entries(values).forEach(([key, value]) => body.append(key, value));
    photos.forEach((file) => body.append("photos", file));
    return api("/auth/register-business", {
      method: "POST",
      body,
      anonymous: true,
    });
  },
  authenticate: (mode, body) =>
    api(`/auth/${mode === "verify" ? "verify-email" : mode}`, {
      method: "POST",
      body,
      anonymous: true,
    }),
  google: (idToken) =>
    api("/auth/google", { method: "POST", body: { idToken }, anonymous: true }),
  resend: (email) =>
    api("/auth/resend-verification-code", {
      method: "POST",
      body: { email },
      anonymous: true,
    }),
  logout: (refreshToken) =>
    api("/auth/logout", { method: "POST", body: { refreshToken } }),
};
