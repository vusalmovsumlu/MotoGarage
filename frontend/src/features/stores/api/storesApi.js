import { api } from "../../../shared/api/client.js";
import { query } from "../../../shared/utils/query.js";

export const storesApi = {
  listPath: (filters) => `/stores${query(filters || {})}`,
  detailPath: (slug) => `/stores/${encodeURIComponent(slug)}`,
  ownPath: () => "/" + "stores" + "/my-profile",
  saveProfile: (existing, body) =>
    api(existing ? "/stores/my-profile" : "/stores", {
      method: existing ? "PUT" : "POST",
      body,
    }),
  deactivateProfile: () => api("/stores/my-profile", { method: "DELETE" }),
};
