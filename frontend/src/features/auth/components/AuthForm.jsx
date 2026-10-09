import { EyeOff, Eye, ArrowRight } from "lucide-react";
import { Button } from "../../../components/ui/Button.jsx";
import { RegistrationPhotos } from "./RegistrationPhotos.jsx";
import { lazy, Suspense } from "react";

const CodeSlots = lazy(() => import("../../../components/ui/CodeSlots.jsx"));

export function AuthForm({
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
  setCode,
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
}) {
  return (
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
                <label htmlFor="auth-first-name">Ad</label>
                <input
                  id="auth-first-name"
                  autoComplete="given-name"
                  required
                  maxLength={100}
                  value={firstName}
                  onChange={(event) => setFirstName(event.target.value)}
                />
              </div>
              <div className="field">
                <label htmlFor="auth-last-name">Soyad</label>
                <input
                  id="auth-last-name"
                  autoComplete="family-name"
                  required
                  maxLength={100}
                  value={lastName}
                  onChange={(event) => setLastName(event.target.value)}
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
                  <option value="Mechanic">Usta — servis və görüşlər</option>
                  <option value="Store">Mağaza — məhsul və sifarişlər</option>
                </select>
              </div>
            </>
          )}
          {mode === "register" && ["Mechanic", "Store"].includes(role) && (
            <RegistrationPhotos
              role={role}
              photos={photos}
              setPhotos={setPhotos}
            />
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
              <Suspense
                fallback={
                  <div
                    className="skeleton-block"
                    style={{ height: 52 }}
                    role="status"
                    aria-label="Kod sahəsi yüklənir"
                  />
                }
              >
                <CodeSlots
                  id="auth-code"
                  length={6}
                  ariaLabel="Təsdiqləmə kodu"
                  autoFocus
                  disabled={busy}
                  status={otpStatus}
                  value={code}
                  onChange={setCode}
                  accentColor="var(--primary)"
                  inkColor="var(--text)"
                  slotColor="var(--control)"
                  digitColor="#fff"
                  dangerColor="var(--red)"
                />
              </Suspense>
            </div>
          ) : (
            <div className="field">
              <label htmlFor="auth-password">Şifrə</label>
              <div className="password-field">
                <input
                  id="auth-password"
                  type={show ? "text" : "password"}
                  autoComplete={
                    mode === "register" ? "new-password" : "current-password"
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
        <Button
          className="full-width"
          type="submit"
          busy={busy}
          disabled={mode === "verify" && otpStatus === "error"}
        >
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
          onClick={resendCode}
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
  );
}
