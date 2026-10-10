import { Resource } from "../../../components/feedback/Resource.jsx";
import { navigate } from "../../../app/router/useRoute.js";
import { MessageCircle } from "lucide-react";
import { date } from "../../../shared/utils/format.js";
import { Empty } from "../../../components/feedback/Empty.jsx";

export function ConversationList({ conversations, active, title, role }) {
  const business = ["Mechanic", "Store"].includes(role);
  return (
    <aside className="conversation-sidebar">
      <div className="section-heading">
        <h2>Söhbətləriniz</h2>
        <span className="meta">{conversations.data?.length || 0}</span>
      </div>
      <Resource state={conversations} skeleton="compact">
        {conversations.data?.length ? (
          conversations.data.map((item) => (
            <button
              className={`conversation-item ${item.id === active ? "active" : ""}`}
              key={item.id}
              onClick={() => navigate(`/chat/${item.id}`)}
            >
              <span className="conversation-avatar">
                <MessageCircle size={18} />
              </span>
              <span>
                <strong>{title(item)}</strong>
                <small className="meta">
                  {date(item.lastMessageAt || item.createdAt)}
                </small>
              </span>
            </button>
          ))
        ) : (
          <Empty
            title="Söhbət yoxdur"
            description={
              business
                ? "Müştərilərinizin mesajları burada görünəcək."
                : "Usta və ya mağaza profilindən mesaj yazın."
            }
            action={
              !business && (
                <a href="#/mechanics" className="text-link">
                  Ustalara bax
                </a>
              )
            }
          />
        )}
      </Resource>
    </aside>
  );
}
