import { useEffect, useRef, useState } from "react";
import { Camera, X } from "lucide-react";
import "./RegistrationPhotos.css";

function PhotoPreview({ file, index }) {
  const image = useRef(null);
  useEffect(() => {
    const url = URL.createObjectURL(file);
    image.current.src = url;
    return () => URL.revokeObjectURL(url);
  }, [file]);
  return <img ref={image} alt={`Seçilmiş şəkil ${index + 1}`} />;
}

export function RegistrationPhotos({ role, photos, setPhotos }) {
  const [error, setError] = useState("");
  const choose = (event) => {
    const selected = Array.from(event.target.files || []);
    event.target.value = ""; // Allow choosing the same file again after removing it.
    if (!selected.length) return;
    setError("");
    if (
      selected.some(
        (file) =>
          !file.size ||
          file.size > 5 * 1024 * 1024 ||
          !/\.(jpe?g|png|webp)$/i.test(file.name) ||
          !/^image\/(jpeg|png|webp)$/.test(file.type),
      )
    ) {
      setError("JPG, PNG və ya WEBP seçin. Hər şəkil maksimum 5 MB ola bilər.");
      return;
    }
    const next = selected.length === 3 ? [null, null, null] : [...photos];
    if (selected.length > next.filter((file) => !file).length) {
      setError(
        "Cəmi 3 şəkil seçin. Dəyişmək üçün əvvəl şəkli silin və ya 3 yeni şəkli birlikdə seçin.",
      );
      return;
    }
    const existing = next.filter(Boolean);
    const key = (file) => `${file.name}:${file.size}:${file.lastModified}`;
    if (
      new Set([...existing, ...selected].map(key)).size !==
      existing.length + selected.length
    ) {
      setError("3 fərqli şəkil seçin. Eyni şəkli təkrar əlavə etmək olmaz.");
      return;
    }
    selected.forEach((file) => {
      next[next.indexOf(null)] = file;
    });
    setPhotos(next);
  };
  return (
    <div className="registration-evidence">
      <p className="registration-evidence-title">
        3 real şəkil <span className="required">*</span>
      </p>
      <p className="meta">
        {role === "Mechanic"
          ? "İşlədiyiniz qarajın və ya iş yerinin 3 fərqli şəklini əlavə edin."
          : "Mağazada olan real detalların 3 fərqli şəklini əlavə edin."}{" "}
        Admin şəkillərə baxıb müraciətinizə qərar verəcək.
      </p>
      <div className="registration-photo-picker">
        <label htmlFor="registration-photos" className="button secondary">
          <Camera size={16} aria-hidden="true" /> Şəkilləri seç
        </label>
        <input
          id="registration-photos"
          type="file"
          multiple
          accept="image/jpeg,image/png,image/webp"
          onChange={choose}
        />
        <span className="meta" role="status">
          {photos.filter(Boolean).length} / 3 şəkil seçilib
        </span>
      </div>
      <div className="registration-photo-slots">
        {photos.map((file, index) => (
          <div className="registration-photo-slot" key={index}>
            <div className="registration-photo-preview">
              {file ? (
                <PhotoPreview file={file} index={index} />
              ) : (
                <Camera size={23} />
              )}
              <span>Şəkil {index + 1}</span>
            </div>
            {file && (
              <button
                type="button"
                className="registration-photo-remove"
                aria-label={`Şəkil ${index + 1} sil`}
                onClick={() => {
                  setError("");
                  setPhotos(
                    photos.map((current, slot) =>
                      slot === index ? null : current,
                    ),
                  );
                }}
              >
                <X size={14} />
              </button>
            )}
          </div>
        ))}
      </div>
      <small className="meta">
        Şəkilləri birlikdə seçə bilərsiniz. JPG, PNG, WEBP · hər şəkil maksimum
        5 MB
      </small>
      {error && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
    </div>
  );
}
