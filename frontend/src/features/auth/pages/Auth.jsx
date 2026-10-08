import { useAuth } from "../hooks/useAuth.js";
import { Wrench, Sun, Moon } from "lucide-react";
import { AuthForm } from "../components/AuthForm.jsx";

export default function Auth({ route }) {
  const {
    toggleTheme,
    theme,
    mode,
    submit,
    busy,
    fullName,
    setFullName,
    role,
    setRole,
    email,
    setEmail,
    code,
    setCode,
    show,
    password,
    setPassword,
    setShow,
    error,
    notice,
    clientId,
    google,
    resendCode,
  } = useAuth({ route });
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
        <AuthForm
          mode={mode}
          submit={submit}
          busy={busy}
          fullName={fullName}
          setFullName={setFullName}
          role={role}
          setRole={setRole}
          email={email}
          setEmail={setEmail}
          code={code}
          setCode={setCode}
          show={show}
          password={password}
          setPassword={setPassword}
          setShow={setShow}
          error={error}
          notice={notice}
          clientId={clientId}
          google={google}
          resendCode={resendCode}
        />
      </main>
      <footer className="auth-footer">
        MotoGarage · Yolunuzun hər addımında.
      </footer>
    </div>
  );
}
