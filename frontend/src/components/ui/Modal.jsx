import { useRef, useId, useEffect } from "react";

import { IconButton } from "./IconButton.jsx";

import { X } from "lucide-react";

export function Modal({
  title,
  description,
  children,
  onClose,
  busy = false,
  className = "",
}) {
  const ref = useRef(null);
  const id = useId();
  useEffect(() => {
    const dialog = ref.current;
    const previousFocus = document.activeElement;
    dialog.showModal();
    return () => {
      dialog.close();
      if (previousFocus?.isConnected)
        previousFocus.focus({ preventScroll: true });
    };
  }, []);
  return (
    <dialog
      ref={ref}
      className={`dialog ${className}`}
      aria-labelledby={id}
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) onClose();
      }}
      onClick={(event) => {
        if (event.target === ref.current && !busy) onClose();
      }}
    >
      <div className="dialog-inner">
        <header className="dialog-header">
          <div>
            <h2 id={id}>{title}</h2>
            {description && <p className="muted">{description}</p>}
          </div>
          <IconButton label="Bağla" onClick={onClose} disabled={busy}>
            <X size={19} />
          </IconButton>
        </header>
        {children}
      </div>
    </dialog>
  );
}
