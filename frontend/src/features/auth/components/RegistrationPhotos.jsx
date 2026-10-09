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
  const inputs = useRef([]);
  const change = (index, file) => {
    setError("");
    if (
      file &&
      (file.size > 5 * 1024 * 1024 || !/\.(jpe?g|png|webp)$/i.test(file.name))
    ) {
      setError("JPG, PNG və ya WEBP seçin. Hər şəkil maksimum 5 MB ola bilər.");
      file = null;
    }
    if (!file && inputs.current[index]) inputs.current[index].value = "";
    setPhotos(photos.map((current, slot) => (slot === index ? file : current)));
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
      <div className="registration-photo-slots">
        {photos.map((file, index) => (
          <div className="registration-photo-slot" key={index}>
            <label htmlFor={`registration-photo-${index}`}>
              {file ? (
                <PhotoPreview file={file} index={index} />
              ) : (
                <Camera size={23} />
              )}
              <span>Şəkil {index + 1}</span>
            </label>
            <input
              ref={(element) => {
                inputs.current[index] = element;
              }}
              id={`registration-photo-${index}`}
              type="file"
              required
              accept="image/jpeg,image/png,image/webp"
              onChange={(event) =>
                change(index, event.target.files?.[0] || null)
              }
            />
            {file && (
              <button
                type="button"
                className="registration-photo-remove"
                aria-label={`Şəkil ${index + 1} sil`}
                onClick={() => change(index, null)}
              >
                <X size={14} />
              </button>
            )}
          </div>
        ))}
      </div>
      <small className="meta">
        JPG, PNG, WEBP · hər şəkil maksimum 5 MB · 3 fərqli şəkil
      </small>
      {error && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
    </div>
  );
}
