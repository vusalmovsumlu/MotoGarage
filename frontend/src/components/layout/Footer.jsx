import { ArrowUpRight, CodeXml } from "lucide-react";
import "./Footer.css";

const author = "Vüsal Mövsümlü";

export default function Footer({ year }) {
  return (
    <footer className="site-footer footer-authorship">
      <div className="footer-brand">
        <strong>MotoGarage</strong>
        <span>Avtomobil və motosikletinizin qayğısına qalırıq.</span>
      </div>
      <a
        className="footer-author"
        href="https://github.com/vusalmovsumlu"
        target="_blank"
        rel="noopener noreferrer"
        aria-label="Vüsal Mövsümlü — GitHub profili (yeni tabda)"
      >
        <span className="footer-author-caption">Hazırlayan</span>
        <span className="footer-author-signature" aria-hidden="true">
          <CodeXml size={16} className="footer-author-code" />
          <span className="footer-author-name">
            {[...author].map((letter, index) => (
              <span
                className="footer-author-letter"
                style={{ "--letter": index }}
                key={index}
              >
                {letter === " " ? "\u00a0" : letter}
              </span>
            ))}
          </span>
          <ArrowUpRight size={14} className="footer-author-arrow" />
        </span>
      </a>
      <small className="footer-copyright">© {year} MotoGarage</small>
    </footer>
  );
}
