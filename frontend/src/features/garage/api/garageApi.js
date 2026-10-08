import { api } from "../../../shared/api/client.js";

export const garageApi = {
  listPath: () => "/Vehicle",
  detailPath: (id) => `/Vehicle/${id}`,
  slugPath: (slug) => `/Vehicle/by-slug/${encodeURIComponent(slug)}`,
  save: (edit, body) =>
    api(edit.id ? `/Vehicle/${edit.id}` : "/Vehicle", {
      method: edit.id ? "PUT" : "POST",
      body,
    }),
  remove: (id) => api(`/Vehicle/${id}`, { method: "DELETE" }),
};
