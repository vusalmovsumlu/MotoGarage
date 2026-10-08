import { api } from "../../../shared/api/client.js";
import { query } from "../../../shared/utils/query.js";

export const notificationsApi = {
  listPath: (unreadOnly) => `/notifications${query({ unreadOnly })}`,
  unreadCountPath: () => "/notifications/unread-count",
  readAll: () => api("/notifications/read-all", { method: "PATCH" }),
  read: (id) => api(`/notifications/${id}/read`, { method: "PATCH" }),
  remove: (id) => api(`/notifications/${id}`, { method: "DELETE" }),
  removeAll: () => api("/notifications", { method: "DELETE" }),
};
