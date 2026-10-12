import { useState } from "react";
import { useResource } from "../../../shared/hooks/useResource.js";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { appointmentsApi } from "../api/appointmentsApi.js";
import { bakuDay, bakuTime, shiftDay } from "../../../shared/utils/bakuTime.js";

export function AvailabilityPicker({
  mechanicId,
  durationMinutes = 60,
  id,
  value,
  onChange,
}) {
  const [day, setDay] = useState(
    value ? bakuDay(value) : shiftDay(bakuDay(), 1),
  );
  const state = useResource(
    mechanicId && day
      ? appointmentsApi.availabilityPath(mechanicId, day, durationMinutes)
      : null,
  );
  const slots = state.data?.slots || [];
  return (
    <div className="availability-picker">
      <label>
        Görüş günü (Bakı vaxtı)
        <input
          type="date"
          required
          min={bakuDay()}
          max={shiftDay(bakuDay(), 90)}
          value={day}
          onChange={(event) => {
            setDay(event.target.value);
            onChange("");
          }}
        />
      </label>
      <Resource state={state} skeleton="compact">
        <label>
          Boş saat (24 saatlıq)
          <select
            id={id}
            aria-label="Boş saat"
            required
            value={slots.includes(value) ? value : ""}
            onChange={(event) => onChange(event.target.value)}
          >
            <option value="">Saat seçin</option>
            {slots.map((slot) => (
              <option key={slot} value={slot}>
                {bakuTime(slot)}
              </option>
            ))}
          </select>
        </label>
        {!slots.length && (
          <p className="meta" role="status">
            Bu gündə boş vaxt yoxdur. Başqa gün seçin.
          </p>
        )}
      </Resource>
      <small className="meta">
        Müddət: {durationMinutes} dəqiqə. Saatlar Bakı vaxtı ilə göstərilir.
      </small>
    </div>
  );
}
