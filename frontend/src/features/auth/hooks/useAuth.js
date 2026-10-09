import { useApp } from "../../../shared/hooks/useApp.js";
import { useState, useRef, useEffect } from "react";
import { authApi } from "../api/authApi.js";
import { setSession } from "../../../shared/api/client.js";
import { navigate } from "../../../app/router/useRoute.js";

let googleLoader;
let pendingEmail = "";
let pendingNotice = "";
try {
  pendingEmail = sessionStorage.getItem("motogarage.pending-email") || "";
} catch {
  /* Storage is optional. */
}
function rememberEmail(value) {
  pendingEmail = value;
  try {
    if (value) sessionStorage.setItem("motogarage.pending-email", value);
    else sessionStorage.removeItem("motogarage.pending-email");
  } catch {
    /* Preserve the pending email in memory. */
  }
}
function loadGoogle() {
  if (window.google?.accounts) return Promise.resolve(window.google);
  if (!googleLoader)
    googleLoader = new Promise((resolve, reject) => {
      const script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      script.async = true;
      script.onload = () => resolve(window.google);
      script.onerror = () => {
        googleLoader = null;
        script.remove();
        reject(new Error("Google giriş xidməti yüklənmədi."));
      };
      document.head.append(script);
    });
  return googleLoader;
}
export function useAuth({ route }) {
  const mode =
    route === "/register"
      ? "register"
      : route === "/verify"
        ? "verify"
        : "login";
  const { theme, toggleTheme } = useApp();
  const [email, setEmail] = useState(() => pendingEmail);
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [role, setRole] = useState("User");
  const [photos, setPhotos] = useState([null, null, null]);
  const [code, setCode] = useState("");
  const [otpStatus, setOtpStatus] = useState("idle");
  const changeCode = (value) => {
    setCode(value);
    setOtpStatus("idle");
  };
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState(mode === "verify" ? pendingNotice : "");
  const google = useRef(null);
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
  useEffect(() => {
    if (mode !== "login" || !clientId) return;
    let active = true;
    loadGoogle()
      .then((provider) => {
        if (!active) return;
        provider.accounts.id.initialize({
          client_id: clientId,
          callback: async (response) => {
            setBusy(true);
            setError("");
            try {
              const session = await authApi.google(response.credential);
              setSession(session);
              navigate("/home");
            } catch (failure) {
              setError(failure.message);
            } finally {
              setBusy(false);
            }
          },
        });
        provider.accounts.id.renderButton(google.current, {
          theme: theme === "dark" ? "filled_black" : "outline",
          size: "large",
          width: Math.max(
            200,
            Math.min(320, google.current.clientWidth || 320),
          ),
          text: "signin_with",
          shape: "pill",
        });
      })
      .catch((failure) => {
        if (active) setError(failure.message);
      });
    return () => {
      active = false;
    };
  }, [mode, clientId, theme]);
  async function submit(event) {
    event.preventDefault();
    if (busy) return;
    setBusy(true);
    setError("");
    setNotice("");
    try {
      const fullName = `${firstName.trim()} ${lastName.trim()}`;
      if (mode === "register" && (!firstName.trim() || !lastName.trim()))
        throw new Error("Ad və soyadınızı ayrı sahələrdə yazın.");
      if (mode === "register" && fullName.length > 100)
        throw new Error("Ad və soyad birlikdə maksimum 100 simvol ola bilər.");
      if (mode === "verify" && !/^[0-9]{6}$/.test(code))
        throw new Error("6 rəqəmli təsdiqləmə kodunu tam daxil edin.");
      const payload =
        mode === "register"
          ? { fullName, email: email.trim(), password, role }
          : mode === "verify"
            ? { email: email.trim(), code }
            : { email: email.trim(), password };
      const business =
        mode === "register" && ["Mechanic", "Store"].includes(role);
      if (business && photos.some((file) => !file))
        throw new Error("3 şəkil seçmək məcburidir.");
      const response = business
        ? await authApi.registerBusiness(payload, photos)
        : await authApi.authenticate(mode, payload);
      if (mode === "register") {
        rememberEmail(email.trim());
        pendingNotice = response.message || "";
        navigate("/verify");
      } else {
        if (mode === "verify") setOtpStatus("success");
        rememberEmail("");
        pendingNotice = "";
        setPassword("");
        setSession(response);
        navigate("/home");
      }
    } catch (failure) {
      setError(failure.message);
      if (mode === "verify" && /^[0-9]{6}$/.test(code)) setOtpStatus("error");
    } finally {
      setBusy(false);
    }
  }

  const resendCode = async () => {
    setBusy(true);
    setError("");
    try {
      const result = await authApi.resend(email);
      setNotice(result.message);
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  };
  return {
    toggleTheme,
    theme,
    mode,
    submit,
    busy,
    firstName,
    setFirstName,
    lastName,
    setLastName,
    role,
    photos,
    setPhotos,
    setRole,
    email,
    setEmail,
    code,
    setCode: changeCode,
    otpStatus,
    show,
    password,
    setPassword,
    setShow,
    error,
    notice,
    clientId,
    google,
    resendCode,
  };
}
