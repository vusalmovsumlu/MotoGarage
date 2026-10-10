import { useState } from "react";
import { Download, Send } from "lucide-react";
import { Modal } from "../../../components/ui/Modal.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { useApp } from "../../../shared/hooks/useApp.js";
import { garageApi } from "../api/garageApi.js";
import "./VehicleHistoryExport.css";

export function VehicleHistoryExport({ vehicle, onClose }) {
  const [recipientEmail, setRecipientEmail] = useState("");
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(null);
  const [error, setError] = useState("");
  const [sentTo, setSentTo] = useState("");
  const { toast } = useApp();
  async function download() {
    if (busy) return;
    setBusy("download");
    setError("");
    try {
      const blob = await garageApi.exportHistory(vehicle.slug);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `${vehicle.slug}-tarixce.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(() => URL.revokeObjectURL(url), 10000);
      toast("Excel hesabatı yükləndi.");
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(null);
    }
  }
  async function send(event) {
    event.preventDefault();
    if (busy || sentTo) return;
    if (!confirmed) {
      setError("Tarixçənin bu email ünvanına göndərilməsini təsdiqləyin.");
      return;
    }
    setBusy("send");
    setError("");
    try {
      const recipient = recipientEmail.trim();
      await garageApi.sendHistory(vehicle.slug, recipient, true);
      setSentTo(recipient);
      toast("Excel hesabatı email ilə göndərildi.");
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(null);
    }
  }
  return (
    <Modal
      title="Tarixçəni paylaş"
      description={`${vehicle.brand} ${vehicle.model} · ${vehicle.plateNumber}`}
      onClose={onClose}
      busy={Boolean(busy)}
    >
      <p className="muted">
        Excel faylında VIN, dövlət nömrəsi, cari odometr, bütün servis işləri,
        xərclər və yanacaq qeydləri olacaq. Tarixlər Bakı vaxtı ilə, saatlar 24
        saatlıq formatda göstərilir.
      </p>
      <p className="meta">
        Kilometr servis və yanacaq qeydlərində göstərilir. Xərc qeydlərində
        kilometr saxlanmır. Servis, xərc və yanacaq məbləğləri ayrı hesablanır.
      </p>
      <Button
        type="button"
        variant="secondary"
        onClick={download}
        busy={busy === "download"}
        disabled={Boolean(busy)}
      >
        <Download size={16} /> Excel-i yüklə
      </Button>
      <form onSubmit={send} style={{ marginTop: 24 }}>
        <div className="field">
          <label htmlFor="history-recipient">Alıcının emaili</label>
          <input
            id="history-recipient"
            type="email"
            required
            maxLength={254}
            autoComplete="off"
            placeholder="alici@gmail.com"
            value={recipientEmail}
            disabled={Boolean(busy) || Boolean(sentTo)}
            onChange={(event) => {
              setRecipientEmail(event.target.value);
              setConfirmed(false);
            }}
          />
        </div>
        <label className="history-export-consent">
          <input
            type="checkbox"
            required
            checked={confirmed}
            disabled={Boolean(busy) || Boolean(sentTo)}
            onChange={(event) => setConfirmed(event.target.checked)}
          />
          Tarixçənin bu email ünvanına göndərilməsinə razıyam
        </label>
        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        {sentTo && (
          <p className="notice" role="status">
            Excel hesabatı {sentTo} ünvanına göndərildi.
          </p>
        )}
        <footer className="dialog-footer">
          <Button
            type="button"
            variant="secondary"
            disabled={Boolean(busy)}
            onClick={onClose}
          >
            Bağla
          </Button>
          <Button
            type="submit"
            busy={busy === "send"}
            disabled={Boolean(busy) || Boolean(sentTo)}
          >
            <Send size={16} /> Email ilə göndər
          </Button>
        </footer>
      </form>
    </Modal>
  );
}
