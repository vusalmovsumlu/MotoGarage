export function FilterTabs({ items, value, onChange, label }) {
  return (
    <div className="tabs" aria-label={label}>
      {items.map(([key, name]) => (
        <button
          key={String(key)}
          className={value === key ? "active" : ""}
          aria-pressed={value === key}
          onClick={() => onChange(key)}
        >
          {name}
        </button>
      ))}
    </div>
  );
}
