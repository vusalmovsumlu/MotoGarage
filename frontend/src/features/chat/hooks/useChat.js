import { useApp } from "../../../shared/hooks/useApp.js";
import { useResource } from "../../../shared/hooks/useResource.js";
import { chatApi } from "../api/chatApi.js";
import { useState, useRef, useEffect } from "react";
import {
  HubConnectionBuilder,
  LogLevel,
  HubConnectionState,
} from "@microsoft/signalr";
import { HUB_URL } from "../../../shared/config/environment.js";
import { getAccessToken } from "../../../shared/api/client.js";
import { profileApi } from "../../profile/api/profileApi.js";
import { replaceRoute } from "../../../app/router/useRoute.js";
import { conversationPath } from "../utils/conversationRoutes.js";

export function useChat({ selectedId }) {
  const { user, notificationReload, notificationVersion } = useApp();
  const conversations = useResource(chatApi.conversationsPath(), {
    keepPreviousData: true,
  });
  const current = conversations.data?.find(
    (item) => item.slug === selectedId || String(item.id) === selectedId,
  );
  const active =
    current?.id ||
    (conversations.loading && /^\d+$/.test(selectedId || "")
      ? Number(selectedId)
      : null);
  const messages = useResource(active ? chatApi.messagesPath(active) : null);
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
  const title = (item) =>
    item
      ? item.participantName ||
        (item.userId !== user.id
          ? item.userFullName
          : item.mechanicName || item.storeName) ||
        "Söhbət"
      : "Söhbət seçin";
  useEffect(() => {
    if (current?.slug && current.slug !== selectedId)
      replaceRoute(conversationPath(current));
  }, [current, selectedId]);
  useEffect(() => {
    reloadConversations();
  }, [notificationVersion, reloadConversations]);
  useEffect(() => {
    reloadMessages();
  }, [notificationVersion, reloadMessages]);
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
        profileApi.get().catch(() => {});
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
    chatApi
      .read(active, controller.signal)
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
        const result = await chatApi.send(active, content.trim());
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

  return {
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
  };
}
