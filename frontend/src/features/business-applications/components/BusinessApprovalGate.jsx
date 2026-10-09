import { useBusinessApproval } from "../hooks/useBusinessApproval.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { EvidencePhotos } from "./EvidencePhotos.jsx";

export function BusinessApprovalGate({ user, children }) {
  const required =
    ["Mechanic", "Store"].includes(user.role) &&
    user.businessApprovalStatus !== "Approved";
  const state = useBusinessApproval(required);
  const { logout, toggleTheme } = useApp();
  const reload = state.reload;
  if (!required || state.approved) return children;
  const rejected = state.data?.status === "Rejected";
  return (
    <main className="business-approval-screen">
      <div className="actions">
        <strong>MotoGarage</strong>
        <Button variant="ghost" onClick={toggleTheme}>
          Temanı dəyiş
        </Button>
        <Button variant="secondary" onClick={logout}>
          Çıxış et
        </Button>
      </div>
      <Resource state={state} skeleton="detail">
        <div className="card">
          <p className="eyebrow">BİZNES MÜRACİƏTİ</p>
          <h1>
            {rejected ? "Müraciətiniz rədd edildi" : "Admin təsdiqi gözlənilir"}
          </h1>
          <p className="muted">
            {rejected
              ? "Adminin qeydini aşağıda oxuya bilərsiniz."
              : "Emailiniz təsdiqləndi. Admin 3 şəklə baxdıqdan sonra usta və ya mağaza hesabınızı təsdiqləyəcək."}
          </p>
          {state.data?.decisionComment && (
            <p className="business-review-comment">
              {state.data.decisionComment}
            </p>
          )}
          <EvidencePhotos photos={state.data?.photos} />
          {!state.data && (
            <p role="alert">
              Müraciət məlumatı tapılmadı. Administratorla əlaqə saxlayın.
            </p>
          )}
          <Button variant="secondary" onClick={reload}>
            Statusu yenilə
          </Button>
        </div>
      </Resource>
    </main>
  );
}
