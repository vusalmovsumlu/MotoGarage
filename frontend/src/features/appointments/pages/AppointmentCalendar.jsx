import { useState } from "react";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { Badge } from "../../../components/ui/Badge.jsx";
import { FilterTabs } from "../../../components/ui/FilterTabs.jsx";
import { useResource } from "../../../shared/hooks/useResource.js";
import { appointmentsApi } from "../api/appointmentsApi.js";
import { WorkingHoursEditor } from "../components/WorkingHoursEditor.jsx";
import { bakuDay, bakuTime, shiftDay } from "../../../shared/utils/bakuTime.js";
import { date } from "../../../shared/utils/format.js";

export function AppointmentCalendar() {
  const [day, setDay] = useState(bakuDay());
  const [view, setView] = useState("week");
  const [edit, setEdit] = useState(false);
  const end = shiftDay(day, view === "week" ? 6 : 0);
  const state = useResource(appointmentsApi.calendarPath(day, end));
  const hours = useResource(appointmentsApi.hoursPath());
  const days = Array.from({ length: view === "week" ? 7 : 1 }, (_, index) =>
    shiftDay(day, index),
  );
  return (
    <Page
      title="Görüş təqvimi"
      description="Gündəlik və həftəlik planınız · Bakı vaxtı."
      actions={
        <Button disabled={!hours.data} onClick={() => setEdit(true)}>
          İş saatlarını dəyiş
        </Button>
      }
    >
      <div className="planning-toolbar">
        <FilterTabs
          value={view}
          onChange={setView}
          items={[
            ["day", "Günlük"],
            ["week", "Həftəlik"],
          ]}
          label="Təqvim görünüşü"
        />
        <label>
          Başlanğıc günü
          <input
            aria-label="Təqvim başlanğıcı"
            type="date"
            value={day}
            required
            onChange={(event) => {
              if (event.target.value) setDay(event.target.value);
            }}
          />
        </label>
        <a className="button secondary" href="#/work/appointments">
          Görüş siyahısı
        </a>
      </div>
      {hours.error && <Resource state={hours} skeleton="compact" />}
      <Resource state={state}>
        <div
          className={`appointment-calendar ${view === "day" ? "calendar-day" : ""}`}
        >
          {days.map((calendarDay) => (
            <section className="card calendar-column" key={calendarDay}>
              <h2>{date(`${calendarDay}T12:00:00Z`)}</h2>
              {(state.data || [])
                .filter((item) => bakuDay(item.appointmentDate) === calendarDay)
                .map((item) => (
                  <a
                    href="#/work/appointments"
                    className="calendar-booking"
                    key={item.id}
                  >
                    <strong>
                      {bakuTime(item.appointmentDate)} ·{" "}
                      {item.durationMinutes || 60} dəq.
                    </strong>
                    <span>{item.vehicleName}</span>
                    <small>{item.customerName}</small>
                    <Badge value={item.status} />
                  </a>
                ))}
              {!(state.data || []).some(
                (item) => bakuDay(item.appointmentDate) === calendarDay,
              ) && <p className="meta">Görüş yoxdur</p>}
            </section>
          ))}
        </div>
      </Resource>
      {edit && (
        <WorkingHoursEditor
          hours={hours.data}
          onClose={() => setEdit(false)}
          onDone={() => {
            hours.reload();
            state.reload();
          }}
        />
      )}
    </Page>
  );
}
