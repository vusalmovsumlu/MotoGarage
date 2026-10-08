import { useState } from "react";
import { uploadImage } from "../../shared/api/imagesApi.js";
import { ImagePlus, LoaderCircle, Plus } from "lucide-react";

export function ImageUpload({ value, onChange, onBusyChange }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function upload(event) {
    const file = event.target.files[0];
    if (!file) return;
    setError("");
    if (
      !["image/jpeg", "image/png", "image/webp"].includes(file.type) ||
      file.size > 5 * 1024 * 1024
    ) {
      setError("JPG, PNG və WEBP; maksimum 5 MB.");
      event.target.value = "";
      return;
    }
    setBusy(true);
    onBusyChange?.(true);
    try {
      const form = new FormData();
      form.append("file", file);
      const result = await uploadImage(form);
      onChange({ imageUrl: result.url, imagePublicId: result.publicId });
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
      onBusyChange?.(false);
      event.target.value = "";
    }
  }
  return (
    <div className="image-upload">
      <div className="upload-preview">
        {value.imageUrl ? (
          <img src={value.imageUrl} alt="Yüklənən şəkil" />
        ) : (
          <ImagePlus size={25} />
        )}
      </div>
      <div>
        <label className={`button secondary ${busy ? "disabled" : ""}`}>
          {busy ? (
            <LoaderCircle size={16} className="spin" />
          ) : (
            <Plus size={16} />
          )}{" "}
          {busy ? "Yüklənir…" : "Şəkil yüklə"}
          <input
            aria-label="Şəkil seç"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={upload}
            disabled={busy}
            className="visually-hidden"
          />
        </label>
        <p className="meta">JPG, PNG, WEBP · maksimum 5 MB</p>
        {value.imageUrl && (
          <button
            type="button"
            className="text-link danger-text"
            onClick={() => onChange({ imageUrl: null, imagePublicId: null })}
            disabled={busy}
          >
            Şəkli çıxar
          </button>
        )}
        {error && (
          <p role="alert" className="form-error">
            {error}
          </p>
        )}
      </div>
    </div>
  );
}
