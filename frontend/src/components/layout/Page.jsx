import { ArrowLeft } from "lucide-react";

export function Page({
  eyebrow = "MOTOGARAGE",
  title,
  description,
  actions,
  children,
}) {
  return (
    <div className="page">
      <a href="#/home" className="back-link">
        <ArrowLeft size={15} /> İş masasına qayıt
      </a>
      <header className="page-heading">
        <div>
          <p className="eyebrow">{eyebrow}</p>
          <h1>{title}</h1>
          {description && <p className="muted">{description}</p>}
        </div>
        <div className="actions">{actions}</div>
      </header>
      {children}
    </div>
  );
}
