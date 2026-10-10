import { Section } from "../../../components/layout/Section.jsx";
import { ArrowRight } from "lucide-react";
import { Preview } from "./Preview.jsx";
import { date, number, money } from "../../../shared/utils/format.js";

export function DashboardSummary({ admin, data }) {
  return (
    <aside className="summary-column">
      {admin ? (
        <Section title="Diqqət gözləyənlər">
          <div className="summary-row">
            <span>Biznes müraciətləri</span>
            <strong>{number(data.pendingBusinessApplications)}</strong>
          </div>
          <div className="summary-row">
            <span>Bloklanmış hesablar</span>
            <strong>{number(data.blockedUsers)}</strong>
          </div>
          <a className="text-link" href="#/admin/users">
            İstifadəçiləri idarə et <ArrowRight size={14} />
          </a>
          <a className="text-link" href="#/admin/applications">
            Müraciətlərə bax <ArrowRight size={14} />
          </a>
        </Section>
      ) : (
        <>
          <Section title="Növbəti görüşlər" link="/appointments">
            <Preview
              items={data.upcomingAppointments}
              empty="Hazırda planlaşdırılmış görüşünüz yoxdur."
              render={(item) => (
                <a href="#/appointments" key={item.id} className="preview-row">
                  <span className="preview-time">
                    {date(item.appointmentDate, true)}
                  </span>
                  <strong>
                    {item.description || `Servis görüşü #${item.id}`}
                  </strong>
                  <span className="meta">
                    {item.mechanicName || "Usta adı yoxdur"}
                  </span>
                </a>
              )}
            />
          </Section>
          <Section title="Yadda saxlayın" link="/records/reminders">
            <Preview
              items={data.upcomingReminders}
              empty="Aktiv xatırlatma yoxdur. Yolunuz rahat olsun."
              render={(item) => (
                <a
                  href={`#/records/reminders?vehicle=${item.vehicleId}`}
                  key={item.id}
                  className="preview-row"
                >
                  <strong>{item.title}</strong>
                  <span className="meta">
                    {date(item.reminderDate)}
                    {item.reminderKm != null &&
                      ` · ${number(item.reminderKm)} km`}
                  </span>
                </a>
              )}
            />
          </Section>
          <Section title="Son yanacaq qeydləri" link="/records/fuel">
            <Preview
              items={data.recentFuelRecords}
              empty="İlk doldurmanı qeyd edib sərfiyyatı izləyin."
              render={(item) => (
                <a
                  href={`#/records/fuel?vehicle=${item.vehicleId}`}
                  key={item.id}
                  className="preview-row"
                >
                  <strong>
                    {number(item.liters)} litr{" "}
                    <span className="row-amount">{money(item.totalPrice)}</span>
                  </strong>
                  <span className="meta">
                    {date(item.fuelDate)} · {number(item.kilometer)} km
                  </span>
                </a>
              )}
            />
          </Section>
        </>
      )}
    </aside>
  );
}
