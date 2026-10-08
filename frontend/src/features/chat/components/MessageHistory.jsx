import { Resource } from "../../../components/feedback/Resource.jsx";
import { date } from "../../../shared/utils/format.js";
import { CheckCheck, MessageCircle } from "lucide-react";
import { Empty } from "../../../components/feedback/Empty.jsx";

export function MessageHistory({
  scroll,
  messages,
  history,
  user,
  readThrough,
}) {
  return (
    <div
      ref={scroll}
      className="message-history"
      role="log"
      aria-label="Söhbət tarixçəsi"
    >
      <Resource state={messages} skeleton="messages">
        {history.length ? (
          history.map((item) => (
            <div
              key={item.id}
              className={`message ${item.senderUserId === user.id ? "outgoing" : "incoming"}`}
            >
              <p>{item.content}</p>
              <span>
                {date(item.sentAt, true)}
                {item.senderUserId === user.id && (
                  <CheckCheck
                    size={13}
                    className={
                      item.status === "Read" || item.id <= readThrough
                        ? "read"
                        : ""
                    }
                  />
                )}
              </span>
            </div>
          ))
        ) : (
          <Empty
            icon={MessageCircle}
            title="Söhbətə başlayın"
            description="İlk mesajı yazın."
          />
        )}
      </Resource>
    </div>
  );
}
