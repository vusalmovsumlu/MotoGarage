import { useState } from "react";
import { Button } from "../../../components/ui/Button.jsx";
import { Modal } from "../../../components/ui/Modal.jsx";
import { useApp } from "../../../shared/hooks/useApp.js";
import { date } from "../../../shared/utils/format.js";
import { appointmentsApi } from "../api/appointmentsApi.js";
import { AvailabilityPicker } from "./AvailabilityPicker.jsx";
import { ServiceReportDialog } from "./ServiceReportDialog.jsx";

export function AppointmentActions({ item, incoming, onDone }) {
  const [dialog, setDialog] = useState(null);
  const [time, setTime] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const { user, toast } = useApp();
  async function run(action, message) {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await action();
      onDone();
      toast(message);
      setDialog(null);
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }
  const active = ["Pending", "Accepted"].includes(item.status);
  const proposal = active && item.proposedAppointmentDateUtc;
  const ownProposal =
    Number(item.rescheduleProposedByUserId) === Number(user.id);
  return (
    <div className="appointment-tools">
      {proposal ? (
        <div className="proposal-card">
          <p>
            Yeni vaxt təklifi:{" "}
            <strong>{date(item.proposedAppointmentDateUtc, true)}</strong> (Bakı
            vaxtı)
          </p>
          {ownProposal ? (
            <>
              <p className="meta">
                Qarşı tərəfin cavabı gözlənilir. Əvvəlki vaxt qüvvədədir.
              </p>
              <Button
                variant="secondary"
                busy={busy}
                onClick={() =>
                  run(
                    () =>
                      appointmentsApi.decideReschedule(item.id, {
                        revision: item.rescheduleRevision,
                        approve: false,
                      }),
                    "Vaxt təklifi geri götürüldü.",
                  )
                }
              >
                Təklifi geri götür
              </Button>
            </>
          ) : (
            <div className="work-actions">
              <Button
                busy={busy}
                onClick={() =>
                  run(
                    () =>
                      appointmentsApi.decideReschedule(item.id, {
                        revision: item.rescheduleRevision,
                        approve: true,
                      }),
                    "Görüş vaxtı yeniləndi.",
                  )
                }
              >
                Yeni vaxtı qəbul et
              </Button>
              <Button
                variant="secondary"
                disabled={busy}
                onClick={() =>
                  run(
                    () =>
                      appointmentsApi.decideReschedule(item.id, {
                        revision: item.rescheduleRevision,
                        approve: false,
                      }),
                    "Vaxt təklifi rədd edildi.",
                  )
                }
              >
                Vaxtı rədd et
              </Button>
            </div>
          )}
        </div>
      ) : (
        active && (
          <Button
            variant="secondary"
            onClick={() => {
              setTime("");
              setError("");
              setDialog("reschedule");
            }}
          >
            Vaxt dəyişikliyi təklif et
          </Button>
        )
      )}
      {item.status === "Completed" && (
        <Button
          variant="secondary"
          onClick={() => {
            setError("");
            setDialog("report");
          }}
        >
          {incoming ? "Servis hesabatı" : "Servis hesabatına bax"}
        </Button>
      )}
      {error && dialog !== "reschedule" && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {dialog === "reschedule" && (
        <Modal
          title="Yeni görüş vaxtı təklif edin"
          description="Qarşı tərəf qəbul edənədək hazırkı vaxt dəyişmir."
          onClose={() => setDialog(null)}
          busy={busy}
        >
          <form
            onSubmit={(event) => {
              event.preventDefault();
              if (time)
                run(
                  () =>
                    appointmentsApi.propose(item.id, { appointmentDate: time }),
                  "Yeni vaxt qarşı tərəfə göndərildi.",
                );
            }}
          >
            <AvailabilityPicker
              mechanicId={item.mechanicId}
              durationMinutes={item.durationMinutes || 60}
              value={time}
              onChange={setTime}
            />
            {error && (
              <p role="alert" className="form-error">
                {error}
              </p>
            )}
            <footer className="dialog-footer">
              <Button
                variant="secondary"
                type="button"
                disabled={busy}
                onClick={() => setDialog(null)}
              >
                İmtina
              </Button>
              <Button type="submit" busy={busy} disabled={!time}>
                Təklif göndər
              </Button>
            </footer>
          </form>
        </Modal>
      )}
      {dialog === "report" && (
        <ServiceReportDialog
          item={item}
          incoming={incoming}
          onClose={() => setDialog(null)}
          onDone={onDone}
        />
      )}
    </div>
  );
}
