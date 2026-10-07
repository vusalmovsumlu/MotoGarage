import { useEffect, useRef, useState } from "react";
import { ArrowRight, Eye, EyeOff, Moon, Sun, Wrench } from "lucide-react";
import { api, setSession } from "../lib/api";
import { navigate } from "../lib/hooks";
import { Button } from "../components/ui";
import { useApp } from "../context";

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
export default function Auth({ route }) {
  const mode =
    route === "/register"
      ? "register"
      : route === "/verify"
        ? "verify"
        : "login";
  const { theme, toggleTheme } = useApp();
  const [email, setEmail] = useState(() => pendingEmail);
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [role, setRole] = useState("User");
  const [code, setCode] = useState("");
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
              const session = await api("/auth/google", {
                method: "POST",
                body: { idToken: response.credential },
                anonymous: true,
              });
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
      const payload =
        mode === "register"
          ? { fullName: fullName.trim(), email: email.trim(), password, role }
          : mode === "verify"
            ? { email: email.trim(), code }
            : { email: email.trim(), password };
      const response = await api(
        `/auth/${mode === "verify" ? "verify-email" : mode}`,
        { method: "POST", body: payload, anonymous: true },
      );
      if (mode === "register") {
        rememberEmail(email.trim());
        pendingNotice = response.message || "";
        navigate("/verify");
      } else {
        rememberEmail("");
        pendingNotice = "";
        setPassword("");
        setSession(response);
        navigate("/home");
      }
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="auth-page">
      <header className="auth-header">
        <a className="brand" href="#/login">
          <span className="brand-symbol">
            <Wrench size={17} />
          </span>
          MotoGarage
        </a>
        <button
          className="icon-button"
          aria-label="Temanı dəyiş"
          onClick={toggleTheme}
        >
          {theme === "dark" ? <Sun size={19} /> : <Moon size={19} />}
        </button>
      </header>
      <main className="auth-layout">
        <section className="auth-story">
          <div className="story-kicker">
            <span className="live-dot" /> SİZİN ŞƏXSİ QARAJINIZ
          </div>
          <h1>
            Hər kilometr.
            <br />
            Hər detal.
            <br />
            <span>Bir yerdə.</span>
          </h1>
          <p>
            Avtomobil və motosikletinizin qayğısına qalın, doğru ustanı tapın və
            növbəti yol üçün hazır olun.
          </p>
          <div className="auth-art">
            <img
              src="/icons/garage.png"
              alt="Motosiklet təsvirli qaraj ikonu"
            />
            <div className="floating-note">
              <span className="live-dot" />
              <span>Qarajınız həmişə yanınızda.</span>
            </div>
          </div>
          <div className="story-footer">
            <span>Qaraj</span>
            <i /> <span>Servis</span>
            <i />
            <span>Ehtiyat hissələri</span>
          </div>
        </section>
        <section className="auth-card card">
          <p className="eyebrow">XOŞ GƏLMİSİNİZ</p>
          <h2>
            {mode === "register"
              ? "Birlikdə yola çıxaq."
              : mode === "verify"
                ? "Emailinizi təsdiqləyin."
                : "Yenidən xoş gördük."}
          </h2>
          <p className="muted">
            {mode === "register"
              ? "Özünüzə uyğun hesabı yaradın."
              : mode === "verify"
                ? "Emailinizə gələn 6 rəqəmli kodu daxil edin."
                : "Qarajınıza davam etmək üçün daxil olun."}
          </p>
          <form onSubmit={submit}>
            <fieldset disabled={busy} className="form-fields">
              {mode === "register" && (
                <>
                  <div className="field">
                    <label htmlFor="auth-name">Ad və soyad</label>
                    <input
                      id="auth-name"
                      autoComplete="name"
                      required
                      maxLength={100}
                      value={fullName}
                      onChange={(event) => setFullName(event.target.value)}
                    />
                  </div>
                  <div className="field">
                    <label htmlFor="auth-role">Hesab növü</label>
                    <select
                      id="auth-role"
                      value={role}
                      onChange={(event) => setRole(event.target.value)}
                    >
                      <option value="User">İstifadəçi — şəxsi qaraj</option>
                      <option value="Mechanic">
                        Usta — servis və görüşlər
                      </option>
                      <option value="Store">
                        Mağaza — məhsul və sifarişlər
                      </option>
                    </select>
                  </div>
                </>
              )}
              <div className="field">
                <label htmlFor="auth-email">Email ünvanı</label>
                <input
                  id="auth-email"
                  type="email"
                  autoComplete="email"
                  required
                  maxLength={256}
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="ad@example.com"
                />
              </div>
              {mode === "verify" ? (
                <div className="field">
                  <label htmlFor="auth-code">Təsdiqləmə kodu</label>
                  <input
                    id="auth-code"
                    className="otp-input"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    required
                    pattern="[0-9]{6}"
                    maxLength={6}
                    value={code}
                    onChange={(event) =>
                      setCode(event.target.value.replace(/\D/g, ""))
                    }
                    placeholder="000000"
                  />
                </div>
              ) : (
                <div className="field">
                  <label htmlFor="auth-password">Şifrə</label>
                  <div className="password-field">
                    <input
                      id="auth-password"
                      type={show ? "text" : "password"}
                      autoComplete={
                        mode === "register"
                          ? "new-password"
                          : "current-password"
                      }
                      required
                      minLength={6}
                      maxLength={100}
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                    />
                    <button
                      type="button"
                      className="icon-button"
                      aria-label={show ? "Şifrəni gizlət" : "Şifrəni göstər"}
                      onClick={() => setShow(!show)}
                    >
                      {show ? <EyeOff size={17} /> : <Eye size={17} />}
                    </button>
                  </div>
                  {mode === "register" && (
                    <small className="meta">
                      Ən azı 6 simvol, kiçik hərf və rəqəm.
                    </small>
                  )}
                </div>
              )}
            </fieldset>
            {error && (
              <p className="form-error" role="alert">
                {error}
              </p>
            )}
            {notice && (
              <p className="notice" role="status">
                {notice}
              </p>
            )}
            <Button className="full-width" type="submit" busy={busy}>
              {mode === "register"
                ? "Hesab yarat"
                : mode === "verify"
                  ? "Təsdiqlə və daxil ol"
                  : "Daxil ol"}
              <ArrowRight size={17} />
            </Button>
          </form>
          {mode === "login" && clientId && (
            <>
              <div className="divider-label">və ya</div>
              <div ref={google} className="google-button" />
            </>
          )}
          {mode === "verify" && (
            <Button
              variant="ghost"
              className="full-width"
              disabled={busy || !email}
              onClick={async () => {
                setBusy(true);
                setError("");
                try {
                  const result = await api("/auth/resend-verification-code", {
                    method: "POST",
                    body: { email },
                    anonymous: true,
                  });
                  setNotice(result.message);
                } catch (failure) {
                  setError(failure.message);
                } finally {
                  setBusy(false);
                }
              }}
            >
              Yeni kod göndər
            </Button>
          )}
          <p className="auth-switch">
            {mode === "register"
              ? "Artıq hesabınız var?"
              : mode === "verify"
                ? "Emailiniz təsdiqlənib?"
                : "Hələ hesabınız yoxdur?"}{" "}
            <a href={mode === "login" ? "#/register" : "#/login"}>
              {mode === "login" ? "Qeydiyyatdan keçin" : "Daxil olun"}
            </a>
          </p>
          {mode === "login" && (
            <a className="verify-link" href="#/verify">
              Email təsdiqləməsini davam etdir
            </a>
          )}
        </section>
      </main>
      <footer className="auth-footer">
        MotoGarage · Yolunuzun hər addımında.
      </footer>
    </div>
  );
}
