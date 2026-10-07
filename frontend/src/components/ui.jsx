import { useEffect, useId, useRef, useState } from "react";
import {
  ArrowLeft,
  ArrowUpRight,
  Check,
  ImagePlus,
  LoaderCircle,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  X,
} from "lucide-react";
import { api } from "../lib/api";
import { localInput, labels } from "../lib/format";
import ResourceSkeleton from "./Skeleton";
import PeekRating from "./PeekRating";

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
export function IconButton({ label, children, ...props }) {
  return (
    <button className="icon-button" aria-label={label} title={label} {...props}>
      {children}
    </button>
  );
}
export function Page({
  eyebrow = "MOTOGARAGE",
  title,
  description,
  actions,
  children,
}) {
  return (
    <div className="page">
      <a href="#/home" className="back-link">
        <ArrowLeft size={15} /> İş masasına qayıt
      </a>
      <header className="page-heading">
        <div>
          <p className="eyebrow">{eyebrow}</p>
          <h1>{title}</h1>
          {description && <p className="muted">{description}</p>}
        </div>
        <div className="actions">{actions}</div>
      </header>
      {children}
    </div>
  );
}
export function Empty({
  icon: Icon,
  title = "Hələ qeyd yoxdur",
  description = "İlk qeydi əlavə edərək başlayın.",
  action,
}) {
  return (
    <div className="empty">
      {Icon && (
        <span className="empty-icon">
          <Icon size={25} />
        </span>
      )}
      <h3>{title}</h3>
      <p className="muted">{description}</p>
      {action}
    </div>
  );
}
export function Resource({
  state,
  children,
  allow404 = false,
  skeleton = "list",
}) {
  if (state.loading) return <ResourceSkeleton variant={skeleton} />;
  if (state.error && !(allow404 && state.error.status === 404))
    return (
      <div className="error-panel" role="alert">
        <h3>Məlumatlar yüklənmədi</h3>
        <p>{state.error.message}</p>
        <Button variant="secondary" onClick={state.reload}>
          <RefreshCw size={16} /> Yenidən cəhd et
        </Button>
      </div>
    );
  return children;
}
export function Badge({ value, children }) {
  return (
    <span className={`badge status-${String(value).toLowerCase()}`}>
      {children || labels[value] || value}
    </span>
  );
}
export function Stat({ label, value, note, icon: Icon }) {
  return (
    <div className="card stat">
      <div className="stat-top">
        <span className="muted">{label}</span>
        {Icon && <Icon size={17} />}
      </div>
      <strong>{value}</strong>
      {note && <span className="meta">{note}</span>}
    </div>
  );
}
export function Section({
  title,
  link,
  linkLabel = "Hamısına bax",
  children,
  className = "",
}) {
  return (
    <section className={`card ${className}`}>
      <div className="section-heading">
        <h2>{title}</h2>
        {link && (
          <a className="text-link" href={`#${link}`}>
            {linkLabel} <ArrowUpRight size={13} />
          </a>
        )}
      </div>
      {children}
    </section>
  );
}
export function SearchBar({
  value,
  onChange,
  placeholder = "Axtar…",
  children,
}) {
  return (
    <div className="filter-bar">
      <div className="search-field">
        <Search size={17} />
        <input
          aria-label={placeholder}
          placeholder={placeholder}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      </div>
      {children}
    </div>
  );
}
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
      const result = await api("/images/upload", {
        method: "POST",
        body: form,
      });
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
export function EntityForm({
  fields,
  initial = {},
  onSave,
  onCancel,
  onBusyChange,
  image = false,
  submitLabel = "Yadda saxla",
}) {
  const [values, setValues] = useState(() =>
    Object.fromEntries(
      fields.map((field) => [
        field.key,
        initial[field.key] !== undefined && initial[field.key] !== null
          ? field.type === "datetime-local"
            ? localInput(initial[field.key])
            : initial[field.key]
          : (field.default ??
            (field.type === "datetime-local" ? localInput() : "")),
      ]),
    ),
  );
  const [asset, setAsset] = useState({
    imageUrl: initial.imageUrl || null,
    imagePublicId: initial.imagePublicId || null,
  });
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");
  const id = useId();
  async function submit(event) {
    event.preventDefault();
    if (busy || uploading) return;
    setError("");
    setBusy(true);
    try {
      const payload = Object.fromEntries(
        fields.map((field) => [
          field.key,
          values[field.key] === "" && !field.required
            ? null
            : ["number", "rating"].includes(field.type)
              ? Number(values[field.key])
              : field.type === "datetime-local"
                ? new Date(values[field.key]).toISOString()
                : typeof values[field.key] === "string"
                  ? values[field.key].trim()
                  : values[field.key],
        ]),
      );
      if (image) Object.assign(payload, asset);
      await onSave(payload);
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form onSubmit={submit}>
      <fieldset disabled={busy} className="form-fields">
        <div className="form-grid">
          {fields.map((field) => (
            <div
              className={`field ${field.wide ? "wide" : ""}`}
              key={field.key}
            >
              <label
                htmlFor={
                  field.type === "rating" ? undefined : `${id}-${field.key}`
                }
              >
                {field.label}
                {field.required && (
                  <span className="required" aria-hidden="true">
                    {" "}
                    *
                  </span>
                )}
              </label>
              {field.type === "rating" ? (
                <div className="rating-field">
                  <PeekRating
                    value={Number(values[field.key])}
                    onChange={(value) =>
                      setValues({ ...values, [field.key]: value })
                    }
                    labels={["Çox zəif", "Zəif", "Orta", "Yaxşı", "Əla"]}
                    ariaLabel={field.label}
                    size={36}
                    lift={8}
                    allowClear={false}
                    disabled={busy || uploading}
                    idleColor="var(--muted)"
                    tipColor="var(--text)"
                    tipTextColor="var(--surface)"
                  />
                  <span className="meta">{values[field.key]} / 5 ulduz</span>
                </div>
              ) : field.options ? (
                <select
                  id={`${id}-${field.key}`}
                  value={values[field.key]}
                  required={field.required}
                  onChange={(event) =>
                    setValues({ ...values, [field.key]: event.target.value })
                  }
                >
                  {!field.required && <option value="">Seçilməyib</option>}
                  {field.options.map((option) => (
                    <option
                      key={option.value || option}
                      value={option.value || option}
                    >
                      {option.label || labels[option] || option}
                    </option>
                  ))}
                </select>
              ) : field.type === "textarea" ? (
                <textarea
                  id={`${id}-${field.key}`}
                  value={values[field.key]}
                  required={field.required}
                  maxLength={field.maxLength}
                  rows={3}
                  onChange={(event) =>
                    setValues({ ...values, [field.key]: event.target.value })
                  }
                />
              ) : (
                <input
                  id={`${id}-${field.key}`}
                  type={field.type || "text"}
                  value={values[field.key]}
                  required={field.required}
                  min={field.min}
                  max={field.max}
                  maxLength={field.maxLength}
                  step={
                    field.step || (field.type === "number" ? "1" : undefined)
                  }
                  placeholder={field.placeholder}
                  autoComplete={field.autoComplete || "off"}
                  onChange={(event) =>
                    setValues({ ...values, [field.key]: event.target.value })
                  }
                />
              )}
              {field.hint && <small className="meta">{field.hint}</small>}
            </div>
          ))}
        </div>
        {image && (
          <ImageUpload
            value={asset}
            onChange={setAsset}
            onBusyChange={(value) => {
              setUploading(value);
              onBusyChange?.(value);
            }}
          />
        )}
      </fieldset>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      <footer className="dialog-footer">
        <Button
          type="button"
          variant="secondary"
          onClick={onCancel}
          disabled={busy || uploading}
        >
          İmtina
        </Button>
        <Button busy={busy} disabled={uploading} type="submit">
          <Check size={16} />
          {submitLabel}
        </Button>
      </footer>
    </form>
  );
}
export function ConfirmDialog({ value, onClose }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  return (
    <Modal
      title={value.title}
      description={value.description}
      onClose={onClose}
      busy={busy}
    >
      <div className="confirm-icon">
        <Trash2 size={24} />
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
          variant="danger"
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
export function Thumbnail({ src, icon: Icon, alt = "" }) {
  const [failedSrc, setFailedSrc] = useState(null);
  return (
    <div className="thumbnail">
      {src && src !== failedSrc ? (
        <img src={src} alt={alt} onError={() => setFailedSrc(src)} />
      ) : (
        Icon && <Icon size={30} strokeWidth={1.5} />
      )}
    </div>
  );
}
