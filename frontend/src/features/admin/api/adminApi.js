import { api } from "../../../shared/api/client.js";
import { query } from "../../../shared/utils/query.js";

export const adminApi = {
  deleteUser: (id) => api(`/admin/users/${id}`, { method: "DELETE" }),
  usersPath: (filters) => `/admin/users${query(filters)}`,
  toggleBlock: (item) =>
    api(`/admin/users/${item.id}/${item.isBlocked ? "unblock" : "block"}`, {
      method: "PATCH",
    }),
};
