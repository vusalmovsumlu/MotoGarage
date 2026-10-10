import { useState } from "react";

import { Modal } from "../ui/Modal.jsx";

import { Trash2, ShieldCheck } from "lucide-react";

import { Button } from "../ui/Button.jsx";

export function ConfirmDialog({ value, onClose }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const approval = value.intent === "approval";
  const Icon = approval ? ShieldCheck : Trash2;
  return (
    <Modal
      title={value.title}
      description={value.description}
      onClose={onClose}
      busy={busy}
    >
      <div
        className={`confirm-icon${approval ? " confirm-icon--approval" : ""}`}
      >
        <Icon size={24} aria-hidden="true" />
      </div>
      {error && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
      <footer className="dialog-footer">
        <Button variant="secondary" onClick={onClose} disabled={busy}>
          İmtina
        </Button>
        <Button
          variant={approval ? "primary" : "danger"}
          busy={busy}
          onClick={async () => {
            setBusy(true);
            setError("");
            try {
              await value.action();
              onClose();
            } catch (failure) {
              setError(failure.message);
            } finally {
              setBusy(false);
            }
          }}
        >
          {value.label || "Təsdiqlə"}
        </Button>
      </footer>
    </Modal>
  );
}
