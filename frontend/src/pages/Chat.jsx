import { useEffect, useRef, useState } from "react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { CheckCheck, MessageCircle, RefreshCw, Send } from "lucide-react";
import { api, getAccessToken, HUB_URL } from "../lib/api";
import { useResource, navigate } from "../lib/hooks";
import { date } from "../lib/format";
import { Button, Empty, Page, Resource } from "../components/ui";
import { useApp } from "../context";

export default function Chat({ selectedId }) {
  const { user, notificationReload } = useApp();
  const conversations = useResource("/chat/conversations");
  const mechanics = useResource("/mechanics");
  const stores = useResource("/stores");
  const active = Number(selectedId) || null;
  const messages = useResource(
    active ? `/chat/conversations/${active}/messages` : null,
  );
  const [liveMessages, setMessages] = useState([]);
  const [content, setContent] = useState("");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const [connectionState, setConnectionState] = useState("connecting");
  const [restart, setRestart] = useState(0);
  const connection = useRef(null);
  const joined = useRef(null);
  const activeRef = useRef(active);
  const scroll = useRef(null);
  useEffect(() => {
    activeRef.current = active;
  }, [active]);
  const [readThrough, setReadThrough] = useState(0);
  const historyRef = useRef([]);
  const history = [
    ...new Map(
      [...(messages.data || []), ...liveMessages].map((item) => [
        item.id,
        item,
      ]),
    ).values(),
  ].sort((a, b) => a.id - b.id);
  const reloadMessages = messages.reload;
  const reloadConversations = conversations.reload;
  useEffect(() => {
    historyRef.current = history;
  });
  const current = conversations.data?.find((item) => item.id === active);
  const title = (item) =>
    !item
      ? "Söhbət seçin"
      : item.userId !== user.id
        ? `Müştəri #${item.userId}`
        : item.mechanicId
          ? mechanics.data?.find((value) => value.id === item.mechanicId)
              ?.businessName || `Usta #${item.mechanicId}`
          : stores.data?.find((value) => value.id === item.storeId)
              ?.storeName || `Mağaza #${item.storeId}`;
  useEffect(() => {
    if (scroll.current) scroll.current.scrollTop = scroll.current.scrollHeight;
  }, [messages.data, liveMessages]);
  useEffect(() => {
    const hub = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.None)
      .build();
    let disposed = false;
    connection.current = hub;
    hub.on("ReceiveMessage", (message) => {
      if (disposed) return;
      reloadConversations();
      notificationReload();
      if (message.conversationId !== activeRef.current) return;
      setMessages((current) =>
        [
          ...new Map(
            [...current, message].map((item) => [item.id, item]),
          ).values(),
        ].sort((a, b) => a.id - b.id),
      );
      if (
        message.senderUserId !== user.id &&
        hub.state === HubConnectionState.Connected
      )
        hub.invoke("MarkAsRead", message.conversationId).catch(() => {});
    });
    hub.on("MessagesRead", (conversationId, readerId) => {
      if (!disposed && conversationId === activeRef.current) {
        if (readerId !== user.id)
          setReadThrough(
            historyRef.current.reduce((max, item) => Math.max(max, item.id), 0),
          );
      }
    });
    hub.onreconnecting(() => {
      if (!disposed) {
        joined.current = null;
        setConnectionState("reconnecting");
      }
    });
    hub.onreconnected(async () => {
      if (disposed) return;
      try {
        if (activeRef.current) {
          await hub.invoke("JoinConversation", activeRef.current);
          joined.current = activeRef.current;
          await hub.invoke("MarkAsRead", activeRef.current);
        }
        setConnectionState("connected");
        reloadMessages();
        reloadConversations();
      } catch {
        setConnectionState("disconnected");
      }
    });
    hub.onclose(() => {
      if (!disposed) {
        joined.current = null;
        setConnectionState("disconnected");
        api("/profile").catch(() => {});
      }
    });
    hub
      .start()
      .then(() => {
        if (!disposed) setConnectionState("connected");
      })
      .catch(() => {
        if (!disposed) setConnectionState("disconnected");
      });
    return () => {
      disposed = true;
      joined.current = null;
      connection.current = null;
      hub.stop().catch(() => {});
    };
  }, [
    restart,
    user.id,
    reloadConversations,
    reloadMessages,
    notificationReload,
  ]);
  useEffect(() => {
    if (!active || connectionState !== "connected") return;
    const hub = connection.current;
    let cancelled = false;
    (async () => {
      try {
        await hub.invoke("JoinConversation", active);
        if (cancelled) {
          await hub.invoke("LeaveConversation", active);
          return;
        }
        joined.current = active;
        await hub.invoke("MarkAsRead", active);
        notificationReload();
      } catch (failure) {
        if (!cancelled)
          setError(failure.message || "Söhbətə qoşulmaq mümkün olmadı.");
      }
    })();
    return () => {
      cancelled = true;
      joined.current = null;
      if (hub.state === HubConnectionState.Connected)
        hub.invoke("LeaveConversation", active).catch(() => {});
    };
  }, [active, connectionState, notificationReload]);
  useEffect(() => {
    if (
      !active ||
      connectionState !== "disconnected" ||
      messages.loading ||
      messages.error
    )
      return;
    const controller = new AbortController();
    api(`/chat/conversations/${active}/read`, {
      method: "PATCH",
      signal: controller.signal,
    })
      .then(notificationReload)
      .catch((failure) => {
        if (failure.name !== "AbortError") setError(failure.message);
      });
    return () => controller.abort();
  }, [
    active,
    connectionState,
    messages.loading,
    messages.error,
    messages.data,
    notificationReload,
  ]);
  async function send(event) {
    event.preventDefault();
    if (!content.trim() || !active || sending) return;
    setSending(true);
    setError("");
    const conversationId = active;
    try {
      if (
        connection.current?.state === HubConnectionState.Connected &&
        joined.current === active
      )
        await connection.current.invoke("SendMessage", active, content.trim());
      else {
        const result = await api(`/chat/conversations/${active}/messages`, {
          method: "POST",
          body: { content: content.trim() },
        });
        if (activeRef.current === conversationId)
          setMessages((current) => [
            ...new Map(
              [...current, result].map((item) => [item.id, item]),
            ).values(),
          ]);
      }
      if (activeRef.current === conversationId) setContent("");
      reloadConversations();
    } catch (failure) {
      if (activeRef.current === conversationId) setError(failure.message);
    } finally {
      setSending(false);
    }
  }
  return (
    <Page title="Söhbətlər" description="Usta və mağazalarla birbaşa əlaqə.">
      <div className="card chat-layout">
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
                description="Usta və ya mağaza profilindən mesaj yazın."
                action={
                  <a href="#/mechanics" className="text-link">
                    Ustalara bax
                  </a>
                }
              />
            )}
          </Resource>
        </aside>
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
              {error && (
                <p className="form-error" role="alert">
                  {error}
                </p>
              )}
              <form className="message-composer" onSubmit={send}>
                <textarea
                  aria-label="Mesajınız"
                  placeholder="Mesajınızı yazın…"
                  value={content}
                  maxLength={2000}
                  rows={2}
                  disabled={
                    sending || messages.loading || Boolean(messages.error)
                  }
                  onChange={(event) => setContent(event.target.value)}
                  onKeyDown={(event) => {
                    if (
                      event.key === "Enter" &&
                      !event.shiftKey &&
                      !event.nativeEvent.isComposing
                    ) {
                      event.preventDefault();
                      event.currentTarget.form.requestSubmit();
                    }
                  }}
                />
                <Button
                  type="submit"
                  busy={sending}
                  disabled={
                    !content.trim() ||
                    messages.loading ||
                    Boolean(messages.error)
                  }
                >
                  <Send size={17} />
                  <span>Göndər</span>
                </Button>
              </form>
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
