import "./DateTime24Input.css";

export default function DateTime24Input({
  id,
  label,
  value,
  required,
  onChange,
}) {
  const [day = "", time = ""] = String(value || "").split("T");
  return (
    <div className="datetime24-control">
      <input
        id={id}
        type="date"
        value={day}
        required={required || Boolean(time)}
        onChange={(event) =>
          onChange(
            event.target.value
              ? `${event.target.value}T${time || "00:00"}`
              : "",
          )
        }
      />
      <div className="datetime24-time">
        <input
          type="text"
          inputMode="numeric"
          aria-label={`${label} — saat (24 saat)`}
          value={time}
          required={required || Boolean(day)}
          placeholder="HH:mm"
          pattern="([01][0-9]|2[0-3]):[0-5][0-9]"
          maxLength={5}
          autoComplete="off"
          title="Saatı 24 saat formatında yazın: 00:00–23:59"
          onChange={(event) => {
            const raw = event.target.value;
            const next = /^\d{3,4}$/.test(raw)
              ? `${raw.slice(0, 2)}:${raw.slice(2)}`
              : raw;
            onChange(`${day}T${next}`);
          }}
        />
        <span className="meta">24 saat · HH:mm</span>
      </div>
    </div>
  );
}
