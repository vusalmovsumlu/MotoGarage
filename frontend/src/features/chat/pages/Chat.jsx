import { useChat } from "../hooks/useChat.js";
import { Page } from "../../../components/layout/Page.jsx";
import { ConversationList } from "../components/ConversationList.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { RefreshCw, MessageCircle } from "lucide-react";
import { MessageHistory } from "../components/MessageHistory.jsx";
import { MessageComposer } from "../components/MessageComposer.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";

export default function Chat({ selectedId }) {
  const {
    conversations,
    active,
    title,
    current,
    connectionState,
    setConnectionState,
    setRestart,
    scroll,
    messages,
    history,
    user,
    readThrough,
    error,
    send,
    content,
    sending,
    setContent,
    media,
  } = useChat({ selectedId });
  return (
    <Page
      title="Söhbətlər"
      description={
        user.role === "User"
          ? "Usta və mağazalarla birbaşa əlaqə."
          : "Müştərilərinizlə birbaşa əlaqə."
      }
    >
      <div className="card chat-layout">
        <ConversationList
          conversations={conversations}
          active={active}
          title={title}
          role={user.role}
        />
        <section className="chat-panel" aria-label="Mesajlar">
          {active ? (
            <>
              <header className="chat-header">
                <div>
                  <h2>{title(current)}</h2>
                  <span className={`connection-status ${connectionState}`}>
                    <i />
                    {
                      {
                        connected: "Canlı bağlantı",
                        connecting: "Qoşulur…",
                        reconnecting: "Yenidən qoşulur…",
                        disconnected:
                          "Canlı bağlantı yoxdur · HTTP mesajlaşma aktivdir",
                      }[connectionState]
                    }
                  </span>
                </div>
                {connectionState === "disconnected" && (
                  <Button
                    variant="ghost"
                    onClick={() => {
                      setConnectionState("connecting");
                      setRestart((value) => value + 1);
                    }}
                  >
                    <RefreshCw size={15} /> Qoşul
                  </Button>
                )}
              </header>
              <MessageHistory
                scroll={scroll}
                messages={messages}
                history={history}
                user={user}
                readThrough={readThrough}
                conversationId={active}
              />
              {error && (
                <p className="form-error" role="alert">
                  {error}
                </p>
              )}
              <MessageComposer
                send={send}
                content={content}
                sending={sending}
                messages={messages}
                setContent={setContent}
                media={media}
              />
            </>
          ) : (
            <Empty
              icon={MessageCircle}
              title="Bir söhbət seçin"
              description="Mesajlarınızı burada oxuya və cavab verə bilərsiniz."
            />
          )}
        </section>
      </div>
    </Page>
  );
}
