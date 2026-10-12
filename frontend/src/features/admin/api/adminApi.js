import { api } from "../../../shared/api/client.js";
import { query } from "../../../shared/utils/query.js";

export const adminApi = {
  auditPath: (page, action) =>
    `/admin/audit-logs${query({ page, pageSize: 25, action })}`,
  deleteUser: (id, reason) =>
    api(`/admin/users/${id}`, {
      method: "DELETE",
      body: reason ? { reason } : undefined,
    }),
  usersPath: (filters) => `/admin/users${query(filters)}`,
  toggleBlock: (item, reason) =>
    api(`/admin/users/${item.id}/${item.isBlocked ? "unblock" : "block"}`, {
      method: "PATCH",
      body: reason ? { reason } : undefined,
    }),
};
