import { useState } from "react";
import { Modal } from "../../../components/ui/Modal.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { useApp } from "../../../shared/hooks/useApp.js";
import { appointmentsApi } from "../api/appointmentsApi.js";

const days = [
  "Bazar",
  "Bazar ertəsi",
  "Çərşənbə axşamı",
  "Çərşənbə",
  "Cümə axşamı",
  "Cümə",
  "Şənbə",
];
export function WorkingHoursEditor({ hours, onClose, onDone }) {
  const [values, setValues] = useState(
    hours.map((hour) => ({
      ...hour,
      start: hour.start.slice(0, 5),
      end: hour.end.slice(0, 5),
    })),
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const { toast } = useApp();
  const change = (index, key, value) =>
    setValues((items) =>
      items.map((hour, position) =>
        position === index ? { ...hour, [key]: value } : hour,
      ),
    );
  return (
    <Modal
      title="İş saatları"
      description="Bakı vaxtı · 24 saatlıq format · mövcud görüşlər saxlanılır."
      onClose={onClose}
      busy={busy}
    >
      <form
        onSubmit={async (event) => {
          event.preventDefault();
          if (busy) return;
          setError("");
          if (values.some((hour) => hour.isOpen && hour.start >= hour.end)) {
            setError("Açıq günün bitmə saatı başlanğıcdan sonra olmalıdır.");
            return;
          }
          setBusy(true);
          try {
            await appointmentsApi.saveHours(
              values.map((hour) => ({
                ...hour,
                start: `${hour.start}:00`,
                end: `${hour.end}:00`,
              })),
            );
            onDone();
            toast("İş saatları yeniləndi.");
            onClose();
          } catch (failure) {
            setError(failure.message);
          } finally {
            setBusy(false);
          }
        }}
      >
        <fieldset disabled={busy} className="form-fields">
          {values.map((hour, index) => (
            <div className="working-hour" key={hour.dayOfWeek}>
              <label>
                <input
                  type="checkbox"
                  checked={hour.isOpen}
                  onChange={(event) =>
                    change(index, "isOpen", event.target.checked)
                  }
                />
                {days[hour.dayOfWeek]}
              </label>
              <div className="hour-inputs">
                <input
                  aria-label={`${days[hour.dayOfWeek]} başlanğıc`}
                  type="text"
                  inputMode="numeric"
                  pattern="(?:[01][0-9]|2[0-3]):(?:00|15|30|45)"
                  placeholder="09:00"
                  required={hour.isOpen}
                  disabled={!hour.isOpen}
                  value={hour.start}
                  onChange={(event) =>
                    change(index, "start", event.target.value)
                  }
                />
                <span>—</span>
                <input
                  aria-label={`${days[hour.dayOfWeek]} bitmə`}
                  type="text"
                  inputMode="numeric"
                  pattern="(?:[01][0-9]|2[0-3]):(?:00|15|30|45)"
                  placeholder="18:00"
                  required={hour.isOpen}
                  disabled={!hour.isOpen}
                  value={hour.end}
                  onChange={(event) => change(index, "end", event.target.value)}
                />
              </div>
            </div>
          ))}
        </fieldset>
        <p className="meta">
          Saatı HH:mm yazın; dəqiqələr 00, 15, 30 və ya 45 ola bilər.
        </p>
        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        <footer className="dialog-footer">
          <Button
            variant="secondary"
            type="button"
            disabled={busy}
            onClick={onClose}
          >
            İmtina
          </Button>
          <Button type="submit" busy={busy}>
            Yadda saxla
          </Button>
        </footer>
      </form>
    </Modal>
  );
}
