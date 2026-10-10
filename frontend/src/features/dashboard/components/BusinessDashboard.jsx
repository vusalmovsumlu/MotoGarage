import {
  CalendarDays,
  CheckCheck,
  Clock,
  Package,
  Truck,
  XCircle,
} from "lucide-react";
import { Stat } from "../../../components/common/Stat.jsx";
import { Section } from "../../../components/layout/Section.jsx";
import { date, money, number } from "../../../shared/utils/format.js";
import { Preview } from "./Preview.jsx";
import { DashboardDestinations } from "./DashboardDestinations.jsx";

export function BusinessDashboard({ user, shortcuts, work }) {
  const mechanic = user.role === "Mechanic";
  const items = work.data || [];
  const path = mechanic ? "/work/appointments" : "/work/orders";
  const metrics = mechanic
    ? [
        ["Yeni görüşlər", ["Pending"], Clock],
        ["Qəbul edilmiş görüşlər", ["Accepted"], CalendarDays],
        ["Tamamlanmış görüşlər", ["Completed"], CheckCheck],
        ["Rədd edilmiş görüşlər", ["Rejected"], XCircle],
      ]
    : [
        ["Yeni sifarişlər", ["Pending"], Clock],
        ["Hazırlanan sifarişlər", ["Confirmed", "Preparing"], Package],
        ["Göndərilən sifarişlər", ["Shipped"], Truck],
        ["Tamamlanmış sifarişlər", ["Completed"], CheckCheck],
      ];
  const active = items
    .filter((item) =>
      (mechanic
        ? ["Pending", "Accepted"]
        : ["Pending", "Confirmed", "Preparing", "Shipped"]
      ).includes(item.status),
    )
    .sort((a, b) =>
      mechanic
        ? new Date(a.appointmentDate) - new Date(b.appointmentDate)
        : new Date(b.createdAt) - new Date(a.createdAt),
    )
    .slice(0, 3);
  return (
    <>
      <div className="stats-grid">
        {metrics.map(([label, statuses, icon]) => (
          <Stat
            key={label}
            label={label}
            value={number(
              items.filter((item) => statuses.includes(item.status)).length,
            )}
            icon={icon}
          />
        ))}
      </div>
      <div className="workspace-columns">
        <aside className="summary-column">
          <Section
            title={
              mechanic ? "Növbəti servis görüşləri" : "Son aktiv sifarişlər"
            }
            link={path}
          >
            <Preview
              items={active}
              empty={
                mechanic
                  ? "Hazırda aktiv servis görüşü yoxdur."
                  : "Hazırda aktiv sifariş yoxdur."
              }
              render={(item) => (
                <a className="preview-row" href={`#${path}`} key={item.id}>
                  <span className="preview-time">
                    {date(
                      mechanic ? item.appointmentDate : item.createdAt,
                      true,
                    )}
                  </span>
                  <strong>
                    {mechanic
                      ? item.description || `Servis görüşü #${item.id}`
                      : `Sifariş #${item.id} · ${money(item.totalAmount)}`}
                  </strong>
                  {mechanic && (
                    <span className="meta">
                      {item.customerName || "Müştəri"} ·{" "}
                      {item.vehicleName || "Nəqliyyat adı yoxdur"}
                    </span>
                  )}
                </a>
              )}
            />
          </Section>
        </aside>
        <DashboardDestinations
          admin={false}
          user={user}
          shortcuts={shortcuts}
          work={work}
        />
      </div>
    </>
  );
}
