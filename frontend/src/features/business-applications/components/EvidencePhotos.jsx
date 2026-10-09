import { useState } from "react";
import { Modal } from "../../../components/ui/Modal.jsx";
import "./EvidencePhotos.css";

export function EvidencePhotos({ photos = [] }) {
  const [selected, setSelected] = useState(null);
  return (
    <>
      <div className="evidence-photo-grid">
        {photos.map((url, index) => (
          <button
            type="button"
            className="evidence-photo"
            key={url}
            aria-label={`Şəkil ${index + 1} böyüt`}
            onClick={() => setSelected(url)}
          >
            <img src={url} alt={`Müraciət şəkli ${index + 1}`} loading="lazy" />
          </button>
        ))}
      </div>
      {selected && (
        <Modal title="Müraciət şəkli" onClose={() => setSelected(null)}>
          <img
            className="evidence-photo-expanded"
            src={selected}
            alt="Seçilmiş müraciət şəkli"
          />
        </Modal>
      )}
    </>
  );
}
