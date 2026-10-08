import { api } from "../../../shared/api/client.js";
import { query } from "../../../shared/utils/query.js";

export const mechanicsApi = {
  listPath: (filters) => `/mechanics${query(filters || {})}`,
  detailPath: (slug) => `/mechanics/${encodeURIComponent(slug)}`,
  ownPath: () => "/" + "mechanics" + "/my-profile",
  saveProfile: (existing, body) =>
    api(existing ? "/mechanics/my-profile" : "/mechanics", {
      method: existing ? "PUT" : "POST",
      body,
    }),
  deactivateProfile: () => api("/mechanics/my-profile", { method: "DELETE" }),
};
