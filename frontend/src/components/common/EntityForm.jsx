import { useState, useId } from "react";

import { localInput } from "../../shared/utils/format.js";

import PeekRating from "../ui/PeekRating.jsx";
import DateTime24Input from "../ui/DateTime24Input.jsx";

import { labels } from "../../shared/constants/labels.js";

import { ImageUpload } from "./ImageUpload.jsx";

import { Button } from "../ui/Button.jsx";

import { Check } from "lucide-react";

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
              {field.render ? (
                field.render({
                  values,
                  value: values[field.key],
                  id: `${id}-${field.key}`,
                  onChange: (value) =>
                    setValues({ ...values, [field.key]: value }),
                })
              ) : field.type === "rating" ? (
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
              ) : field.type === "datetime-local" ? (
                <DateTime24Input
                  id={`${id}-${field.key}`}
                  label={field.label}
                  value={values[field.key]}
                  required={field.required}
                  onChange={(value) =>
                    setValues({ ...values, [field.key]: value })
                  }
                />
              ) : field.options ? (
                <select
                  id={`${id}-${field.key}`}
                  value={values[field.key]}
                  required={field.required}
                  onChange={(event) =>
                    setValues({
                      ...values,
                      ...Object.fromEntries(
                        (field.reset || []).map((key) => [key, ""]),
                      ),
                      [field.key]: event.target.value,
                    })
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
                    setValues({
                      ...values,
                      ...Object.fromEntries(
                        (field.reset || []).map((key) => [key, ""]),
                      ),
                      [field.key]: event.target.value,
                    })
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
                    setValues({
                      ...values,
                      ...Object.fromEntries(
                        (field.reset || []).map((key) => [key, ""]),
                      ),
                      [field.key]: event.target.value,
                    })
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
