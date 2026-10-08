import { ArrowUpRight } from "lucide-react";

export function Section({
  title,
  link,
  linkLabel = "Hamısına bax",
  children,
  className = "",
}) {
  return (
    <section className={`card ${className}`}>
      <div className="section-heading">
        <h2>{title}</h2>
        {link && (
          <a className="text-link" href={`#${link}`}>
            {linkLabel} <ArrowUpRight size={13} />
          </a>
        )}
      </div>
      {children}
    </section>
  );
}
