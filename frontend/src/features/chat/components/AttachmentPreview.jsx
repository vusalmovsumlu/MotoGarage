import { useEffect, useRef } from "react";
import { X } from "lucide-react";

export function AttachmentPreview({ media, disabled }) {
  const preview = useRef(null);
  useEffect(() => {
    if (!media.file) return;
    const objectUrl = URL.createObjectURL(media.file);
    preview.current.src = objectUrl;
    return () => URL.revokeObjectURL(objectUrl);
  }, [media.file]);
  if (!media.file) return null;
  return (
    <div className="chat-attachment-preview">
      {media.file &&
        (media.file.type.startsWith("video/") ? (
          <video ref={preview} controls playsInline preload="metadata" />
        ) : (
          <img ref={preview} alt="Göndəriləcək şəkil" />
        ))}
      <span>
        {media.file.name}
        <small role="status">
          {media.busy ? "Fayl yüklənir…" : "Göndərilməyə hazırdır"}
        </small>
      </span>
      <button
        type="button"
        className="icon-button"
        aria-label="Əlavəni sil"
        disabled={disabled || media.busy || !media.attachment}
        onClick={media.remove}
      >
        <X size={18} />
      </button>
    </div>
  );
}
