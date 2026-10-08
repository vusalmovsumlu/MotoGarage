import { MessageCircle, ArrowRight, ClipboardList } from "lucide-react";
import { Section } from "../../../components/layout/Section.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";

export function DashboardDestinations({ admin, shortcuts, user, work }) {
  const activeWorkCount = work.data?.filter((x) =>
    ["Pending", "Accepted", "Confirmed", "Preparing", "Shipped"].includes(
      x.status,
    ),
  ).length;

  return (
    <div className="destinations-column">
      <div className="section-heading">
        <h2>{admin ? "İdarəetmə" : "Hər şey əlçatan olsun."}</h2>
        <span className="meta">Sizin alətləriniz</span>
      </div>
      <div className="app-shortcuts">
        {shortcuts.map((item) => (
          <a href={`#${item.path}`} className="app-shortcut" key={item.path}>
            {item.art ? (
              <img src={`/icons/${item.art}`} alt="" />
            ) : (
              <span className="shortcut-native">
                <item.icon size={35} strokeWidth={1.5} />
              </span>
            )}
            <strong>{item.title}</strong>
            <span className="meta">{item.description}</span>
          </a>
        ))}
      </div>
      <div className="quick-links">
        <a href="#/chat">
          <MessageCircle size={20} />
          <div>
            <strong>Söhbətlər</strong>
            <span className="meta">Ustanız və mağazanızla əlaqədə qalın.</span>
          </div>
          <ArrowRight size={17} />
        </a>
        <a href="#/orders">
          <ClipboardList size={20} />
          <div>
            <strong>Sifarişlərim</strong>
            <span className="meta">Alışlarınızın gedişatını izləyin.</span>
          </div>
          <ArrowRight size={17} />
        </a>
      </div>
      {["Mechanic", "Store"].includes(user.role) && (
        <Section
          title={
            user.role === "Mechanic" ? "Servis növbəniz" : "Sifariş növbəniz"
          }
          link={
            user.role === "Mechanic" ? "/work/appointments" : "/work/orders"
          }
        >
          <Resource state={work} skeleton="compact">
            {work.data?.filter((x) =>
              [
                "Pending",
                "Accepted",
                "Confirmed",
                "Preparing",
                "Shipped",
              ].includes(x.status),
            ).length ? (
              <p>{activeWorkCount} aktiv iş diqqətinizi gözləyir.</p>
            ) : (
              <p className="quiet-empty">Hazırda aktiv iş yoxdur.</p>
            )}
          </Resource>
        </Section>
      )}
      <div className="workspace-note">
        <span className="live-dot" />
        <p>Bu iş masası hesabınızdakı real məlumatlarla yenilənir.</p>
      </div>
    </div>
  );
}
