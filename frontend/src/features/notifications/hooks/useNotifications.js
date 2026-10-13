import { useEffect } from "react";
import { useResource } from "../../../shared/hooks/useResource.js";
import { notificationsApi } from "../api/notificationsApi.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { navigate } from "../../../app/router/useRoute.js";
import { useUrlQuery } from "../../../shared/hooks/useUrlQuery.js";

export function useNotifications() {
  const { params, update } = useUrlQuery();
  const unread = params.get("unreadOnly") === "true";
  const setUnread = (value) =>
    update({ unreadOnly: value }, { defaults: { unreadOnly: false } });
  const state = useResource(notificationsApi.listPath(unread));
  const reload = state.reload;
  useEffect(() => {
    const interval = setInterval(() => {
      if (!document.hidden) reload();
    }, 30000);
    return () => clearInterval(interval);
  }, [reload]);
  const { act, notificationReload, notificationVersion, user, confirm, toast } =
    useApp();
  useEffect(() => {
    reload();
  }, [notificationVersion, reload]);
  function destination(item) {
    const link = item.link || "";
    if (
      ["/inventory?stock=low", "/inventory?stock=out"].includes(link) &&
      user.role === "Store"
    )
      return link;
    if (link === "/business-application") return "/business";
    if (/^\/garage\/[a-z0-9-]+\/reminders$/.test(link)) return link;
    if (link.startsWith("/chat/")) return link;
    if (link.startsWith("/appointments"))
      return user.role === "Mechanic" ? "/work/appointments" : "/appointments";
    if (link.startsWith("/orders"))
      return user.role === "Store" ? "/work/orders" : "/orders";
    if (link.startsWith("/reminders")) {
      const vehicleId = new URLSearchParams(link.split("?")[1]).get("vehicle");
      return /^\d+$/.test(vehicleId || "") && Number(vehicleId) > 0
        ? `/records/reminders?vehicle=${vehicleId}`
        : "/records/reminders";
    }
    return null;
  }
  const readAll = () =>
    act(async () => {
      await notificationsApi.readAll();
      state.reload();
      notificationReload();
    }, "Bildirişlər oxundu.");
  const visitNotification = (item) =>
    act(async () => {
      if (!item.isRead) await notificationsApi.read(item.id);
      notificationReload();
      navigate(destination(item));
    });
  const readNotification = (item) =>
    act(async () => {
      await notificationsApi.read(item.id);
      state.reload();
      notificationReload();
    });

  const deleteNotification = (item) =>
    confirm({
      title: "Bildirişi silmək istəyirsiniz?",
      description: item.title,
      label: "Sil",
      action: async () => {
        await notificationsApi.remove(item.id);
        state.reload();
        notificationReload();
        toast("Bildiriş silindi.");
      },
    });
  const deleteAllNotifications = () =>
    confirm({
      title: "Bütün bildirişləri silmək istəyirsiniz?",
      description: "Oxunmuş və oxunmamış bütün bildirişləriniz silinəcək.",
      label: "Hamısını sil",
      action: async () => {
        await notificationsApi.removeAll();
        state.reload();
        notificationReload();
        toast("Bildirişlər silindi.");
      },
    });

  return {
    state,
    readAll,
    unread,
    setUnread,
    destination,
    visitNotification,
    readNotification,
    deleteNotification,
    deleteAllNotifications,
  };
}
