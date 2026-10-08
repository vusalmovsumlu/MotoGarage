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
