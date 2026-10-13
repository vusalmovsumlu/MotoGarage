import { useState, useEffect } from "react";
import { useResource } from "../../../shared/hooks/useResource.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { businessApplicationsApi } from "../../business-applications/api/businessApplicationsApi.js";
import { EvidencePhotos } from "../../business-applications/components/EvidencePhotos.jsx";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { Badge } from "../../../components/ui/Badge.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { Modal } from "../../../components/ui/Modal.jsx";
import { date } from "../../../shared/utils/format.js";
import { useUrlQuery } from "../../../shared/hooks/useUrlQuery.js";
import { queryChoice } from "../../../shared/utils/routeQuery.js";

export function BusinessApplications() {
  const { params, update } = useUrlQuery();
  const choice = queryChoice(
    params,
    "status",
    ["Pending", "Approved", "Rejected", "all"],
    "Pending",
  );
  const status = choice === "all" ? "" : choice;
  const setStatus = (value) =>
    update({ status: value || "all" }, { defaults: { status: "Pending" } });
  const state = useResource(businessApplicationsApi.adminPath(status));
  const [decline, setDecline] = useState(null);
  const [comment, setComment] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const { confirm, toast } = useApp();
  const reload = state.reload;
  useEffect(() => {
    const timer = setInterval(() => {
      if (!document.hidden) reload();
    }, 30000);
    return () => clearInterval(timer);
  }, [reload]);
  const accept = (item) =>
    confirm({
      intent: "approval",
      title: "Müraciəti təsdiqləmək istəyirsiniz?",
      description: `${item.fullName} — ${item.role === "Mechanic" ? "Usta" : "Mağaza"}`,
      label: "Təsdiqlə",
      action: async () => {
        await businessApplicationsApi.accept(item.id);
        reload();
        toast("Müraciət təsdiqləndi.");
      },
    });
  async function reject(event) {
    event.preventDefault();
    if (!comment.trim()) {
      setError("Rədd etmə səbəbini yazın.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      await businessApplicationsApi.decline(decline.id, comment.trim());
      setDecline(null);
      reload();
      toast("Müraciət rədd edildi.");
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Page
      title="Biznes müraciətləri"
      eyebrow="ADMİN İDARƏETMƏSİ"
      description="Usta və mağaza qeydiyyatlarını 3 real şəkil əsasında yoxlayın."
    >
      <div className="card filter-bar">
        <label className="field">
          Müraciət statusu
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value)}
          >
            <option value="Pending">Gözləyir</option>
            <option value="Approved">Təsdiqlənib</option>
            <option value="Rejected">Rədd edilib</option>
            <option value="">Hamısı</option>
          </select>
        </label>
      </div>
      <Resource state={state} skeleton="cards">
        {state.data?.length ? (
          state.data.map((item) => (
            <article className="card business-application-card" key={item.id}>
              <div className="actions">
                <h2>{item.fullName}</h2>
                <Badge value={item.status}>
                  {
                    {
                      Pending: "Gözləyir",
                      Approved: "Təsdiqlənib",
                      Rejected: "Rədd edilib",
                    }[item.status]
                  }
                </Badge>
              </div>
              <p className="muted">
                {item.email} · {item.role === "Mechanic" ? "Usta" : "Mağaza"} ·{" "}
                {date(item.createdAt, true)}
              </p>
              <EvidencePhotos photos={item.photos} />
              {item.decisionComment && (
                <p className="business-review-comment">
                  {item.decisionComment}
                </p>
              )}
              {item.status === "Pending" && (
                <div className="actions">
                  <Button onClick={() => accept(item)}>Təsdiqlə</Button>
                  <Button
                    variant="secondary"
                    className="danger-text"
                    onClick={() => {
                      setDecline(item);
                      setComment("");
                      setError("");
                    }}
                  >
                    Rədd et
                  </Button>
                </div>
              )}
            </article>
          ))
        ) : (
          <Empty
            title="Müraciət yoxdur"
            description="Yeni usta və mağaza müraciətləri email təsdiqindən sonra burada görünəcək."
          />
        )}
      </Resource>
      {decline && (
        <Modal
          title="Müraciəti rədd et"
          description={decline.fullName}
          onClose={() => setDecline(null)}
          busy={busy}
        >
          <form onSubmit={reject}>
            <div className="field">
              <label htmlFor="decline-comment">Rədd etmə səbəbi</label>
              <textarea
                id="decline-comment"
                required
                maxLength={1000}
                value={comment}
                disabled={busy}
                onChange={(event) => setComment(event.target.value)}
                rows={4}
              />
            </div>
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
                onClick={() => setDecline(null)}
              >
                İmtina
              </Button>
              <Button type="submit" variant="danger" busy={busy}>
                Rədd et
              </Button>
            </footer>
          </form>
        </Modal>
      )}
    </Page>
  );
}
