import { Bell, Trash2 } from "lucide-react";
import { date } from "../../../shared/utils/format.js";
import { Button } from "../../../components/ui/Button.jsx";

export function NotificationRow({
  item,
  destination,
  visitNotification,
  readNotification,
  deleteNotification,
}) {
  return (
    <div
      className={`notification-row ${!item.isRead ? "unread" : ""}`}
      key={item.id}
    >
      <span className="record-icon">
        <Bell size={19} />
      </span>
      <div className="record-info">
        <strong>{item.title}</strong>
        <p>{item.message}</p>
        <span className="meta">{date(item.createdAt, true)}</span>
      </div>
      <div className="notification-actions">
        {destination(item) && (
          <Button variant="ghost" onClick={() => visitNotification(item)}>
            Bax
          </Button>
        )}
        {!item.isRead && (
          <Button variant="secondary" onClick={() => readNotification(item)}>
            Oxundu
          </Button>
        )}
        <Button
          variant="ghost"
          className="danger-text"
          onClick={() => deleteNotification(item)}
          aria-label={`Bildirişi sil: ${item.title}`}
        >
          <Trash2 size={15} /> Sil
        </Button>
      </div>
    </div>
  );
}
