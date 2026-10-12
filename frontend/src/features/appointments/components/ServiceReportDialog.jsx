import { useState } from "react";
import { Modal } from "../../../components/ui/Modal.jsx";
import { EntityForm } from "../../../components/common/EntityForm.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { useResource } from "../../../shared/hooks/useResource.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { date, money, localInput } from "../../../shared/utils/format.js";
import { appointmentsApi } from "../api/appointmentsApi.js";

const fields = [
  { key: "serviceName", label: "Görülən iş", required: true, maxLength: 100 },
  {
    key: "kilometer",
    label: "Kilometr",
    type: "number",
    required: true,
    min: 0,
    max: 10000000,
  },
  {
    key: "cost",
    label: "Məbləğ (AZN)",
    type: "number",
    required: true,
    min: 0,
    max: 999999999.99,
    step: "0.01",
  },
  {
    key: "serviceDate",
    label: "Servis tarixi və saatı",
    type: "datetime-local",
    required: true,
    default: localInput(),
  },
  {
    key: "description",
    label: "İşin açıqlaması",
    type: "textarea",
    maxLength: 500,
    wide: true,
  },
  {
    key: "replacedParts",
    label: "Dəyişdirilən hissələr",
    type: "textarea",
    maxLength: 1000,
    wide: true,
  },
];

export function ServiceReportDialog({ item, incoming, onClose, onDone }) {
  const state = useResource(appointmentsApi.reportPath(item.id));
  const [busy, setBusy] = useState(false);
  const [comment, setComment] = useState("");
  const [error, setError] = useState("");
  const [initialDate] = useState(() => new Date().toISOString());
  const { toast } = useApp();
  async function decide(approve) {
    if (busy) return;
    if (!approve && !comment.trim()) {
      setError("Rədd səbəbini yazın.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      await appointmentsApi.decideReport(item.id, {
        revision: state.data.revision,
        approve,
        comment,
      });
      onDone();
      toast(
        approve
          ? "Servis tarixçəsinə əlavə edildi."
          : "Hesabat geri göndərildi.",
      );
      onClose();
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }
  const report = state.data;
  return (
    <Modal
      title="Servis hesabatı"
      description={item.vehicleName}
      onClose={onClose}
      busy={busy}
    >
      <Resource state={state} skeleton="form">
        {incoming && report?.status !== "Approved" ? (
          <>
            {report?.decisionComment && (
              <p className="form-error">
                Sahibin qeydi: {report.decisionComment}
              </p>
            )}
            <p className="meta">
              Nəqliyyatın sahibi təsdiq etdikdən sonra servis tarixçəsinə əlavə
              olunacaq.
            </p>
            <EntityForm
              key={report?.revision || 0}
              fields={fields}
              initial={report || { serviceDate: initialDate }}
              onCancel={onClose}
              onBusyChange={setBusy}
              onSave={async (values) => {
                setBusy(true);
                try {
                  await appointmentsApi.submitReport(item.id, {
                    ...values,
                    revision: report?.revision || 0,
                  });
                  onDone();
                  toast("Hesabat sahibə göndərildi.");
                  onClose();
                } finally {
                  setBusy(false);
                }
              }}
              submitLabel="Hesabatı göndər"
            />
          </>
        ) : report ? (
          <>
            <dl className="report-details">
              <dt>Görülən iş</dt>
              <dd>{report.serviceName}</dd>
              <dt>Kilometr</dt>
              <dd>{report.kilometer} km</dd>
              <dt>Məbləğ</dt>
              <dd>{money(report.cost)}</dd>
              <dt>Servis tarixi</dt>
              <dd>{date(report.serviceDate, true)}</dd>
              <dt>Açıqlama</dt>
              <dd>{report.description || "—"}</dd>
              <dt>Dəyişən hissələr</dt>
              <dd>{report.replacedParts || "—"}</dd>
              <dt>Status</dt>
              <dd>
                {
                  {
                    Pending: "Təsdiq gözləyir",
                    Approved: "Tarixçəyə əlavə edilib",
                    Rejected: "Düzəlişə qaytarılıb",
                  }[report.status]
                }
              </dd>
            </dl>
            {report.decisionComment && (
              <p className="meta">Qeyd: {report.decisionComment}</p>
            )}
            {!incoming && report.status === "Pending" && (
              <>
                <label className="field">
                  Rədd edirsinizsə, səbəb
                  <textarea
                    aria-label="Hesabatın rədd səbəbi"
                    maxLength={1000}
                    value={comment}
                    onChange={(event) => setComment(event.target.value)}
                  />
                </label>
                <footer className="dialog-footer">
                  <Button
                    variant="secondary"
                    disabled={busy}
                    onClick={() => decide(false)}
                  >
                    Düzəlişə qaytar
                  </Button>
                  <Button busy={busy} onClick={() => decide(true)}>
                    Təsdiqlə və tarixçəyə əlavə et
                  </Button>
                </footer>
              </>
            )}
          </>
        ) : (
          <p className="meta">Usta hələ servis hesabatı göndərməyib.</p>
        )}
      </Resource>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
    </Modal>
  );
}
