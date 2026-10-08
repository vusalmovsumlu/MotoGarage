import { useNotifications } from "../hooks/useNotifications.js";
import { Page } from "../../../components/layout/Page.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { CheckCheck, Bell, Trash2 } from "lucide-react";
import { FilterTabs } from "../../../components/ui/FilterTabs.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { NotificationRow } from "../components/NotificationRow.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";

const notificationTabs = [
  [false, "Hamısı"],
  [true, "Oxunmamış"],
];
export function Notifications() {
  const {
    state,
    readAll,
    unread,
    setUnread,
    destination,
    visitNotification,
    readNotification,
    deleteNotification,
    deleteAllNotifications,
  } = useNotifications();
  return (
    <Page
      title="Bildirişlər"
      description="Görüş, sifariş və söhbətlərdən xəbərdar olun."
      actions={
        <>
          <Button
            variant="secondary"
            disabled={!state.data?.some((item) => !item.isRead)}
            onClick={readAll}
          >
            <CheckCheck size={16} /> Hamısını oxunmuş et
          </Button>
          <Button
            variant="secondary"
            className="danger-text"
            disabled={!state.data?.length}
            onClick={deleteAllNotifications}
          >
            <Trash2 size={16} /> Hamısını sil
          </Button>
        </>
      }
    >
      <FilterTabs
        items={notificationTabs}
        value={unread}
        onChange={setUnread}
      />
      <Resource state={state}>
        {state.data?.length ? (
          <div className="card rows-card">
            {state.data.map((item) => (
              <NotificationRow
                key={item.id}
                item={item}
                destination={destination}
                visitNotification={visitNotification}
                readNotification={readNotification}
                deleteNotification={deleteNotification}
              />
            ))}
          </div>
        ) : (
          <Empty
            icon={Bell}
            title="Hər şey qaydasındadır"
            description="Yeni bildirişlər burada görünəcək."
          />
        )}
      </Resource>
    </Page>
  );
}
