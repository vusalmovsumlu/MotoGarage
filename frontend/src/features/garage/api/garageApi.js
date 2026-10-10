import { api } from "../../../shared/api/client.js";

export const garageApi = {
  listPath: () => "/Vehicle",
  detailPath: (id) => `/Vehicle/${id}`,
  slugPath: (slug) => `/Vehicle/by-slug/${encodeURIComponent(slug)}`,
  exportHistory: (slug) =>
    api(`/Vehicle/by-slug/${encodeURIComponent(slug)}/history.xlsx`, {
      responseType: "blob",
    }),
  sendHistory: (slug, recipientEmail, confirmSharing) =>
    api(`/Vehicle/by-slug/${encodeURIComponent(slug)}/history/email`, {
      method: "POST",
      body: { recipientEmail, confirmSharing },
    }),
  save: (edit, body) =>
    api(edit.id ? `/Vehicle/${edit.id}` : "/Vehicle", {
      method: edit.id ? "PUT" : "POST",
      body,
    }),
  remove: (id) => api(`/Vehicle/${id}`, { method: "DELETE" }),
};
