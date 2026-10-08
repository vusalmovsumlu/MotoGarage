import { IconButton } from "../ui/IconButton.jsx";
import { X } from "lucide-react";

export function ToastRegion({ notices, setNotices }) {
  return (
    <div className="toast-region" aria-live="polite" aria-atomic="false">
      {notices.map((item) => (
        <div
          className={`toast ${item.error ? "toast-error" : ""}`}
          key={item.id}
          role={item.error ? "alert" : "status"}
        >
          <span>{item.message}</span>
          <IconButton
            label="Bildirişi bağla"
            onClick={() =>
              setNotices((current) =>
                current.filter((value) => value.id !== item.id),
              )
            }
          >
            <X size={16} />
          </IconButton>
        </div>
      ))}
    </div>
  );
}
