import { useEffect } from "react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { HUB_URL } from "../../../shared/config/environment.js";
import { getAccessToken } from "../../../shared/api/client.js";

export function useNotificationRealtime(user, onChange) {
  const id = user?.id;
  const role = user?.role;
  useEffect(() => {
    if (!id || !["User", "Mechanic", "Store"].includes(role)) return;
    const hub = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.None)
      .build();
    let disposed = false;
    let retry;
    let attempt = 0;
    const update = () => {
      if (!disposed) onChange();
    };
    const start = async () => {
      if (disposed || hub.state !== HubConnectionState.Disconnected) return;
      try {
        await hub.start();
        attempt = 0;
        update(); // Catch up with notifications sent during an offline interval.
      } catch {
        if (!disposed)
          retry = setTimeout(
            start,
            Math.min(30000, 2000 * 2 ** Math.min(attempt++, 4)),
          );
      }
    };
    hub.on("NotificationsChanged", update);
    hub.onreconnected(update);
    hub.onclose(() => {
      if (!disposed) retry = setTimeout(start, 2000);
    });
    start();
    const resume = () => {
      if (!document.hidden) {
        update();
        if (hub.state === HubConnectionState.Disconnected) {
          clearTimeout(retry);
          start();
        }
      }
    };
    window.addEventListener("online", resume);
    document.addEventListener("visibilitychange", resume);
    return () => {
      disposed = true;
      clearTimeout(retry);
      window.removeEventListener("online", resume);
      document.removeEventListener("visibilitychange", resume);
      hub.stop().catch(() => {});
    };
  }, [id, role, onChange]);
}
