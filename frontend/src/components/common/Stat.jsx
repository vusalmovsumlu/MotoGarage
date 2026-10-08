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
