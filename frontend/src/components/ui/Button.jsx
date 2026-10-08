import { LoaderCircle } from "lucide-react";

export function Button({
  children,
  variant = "primary",
  busy = false,
  className = "",
  ...props
}) {
  return (
    <button
      className={`button ${variant} ${className}`}
      {...props}
      disabled={props.disabled || busy}
    >
      {busy && <LoaderCircle size={16} className="spin" />}
      {children}
    </button>
  );
}
