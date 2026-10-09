import { api } from "../../../shared/api/client.js";

export const businessApplicationsApi = {
  minePath: () => "/business-applications/my",
  adminPath: (status) =>
    `/admin/business-applications?status=${encodeURIComponent(status)}`,
  accept: (id) =>
    api(`/admin/business-applications/${id}/accept`, { method: "POST" }),
  decline: (id, comment) =>
    api(`/admin/business-applications/${id}/decline`, {
      method: "POST",
      body: { comment },
    }),
};
