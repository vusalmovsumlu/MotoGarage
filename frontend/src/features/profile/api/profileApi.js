import { api } from "../../../shared/api/client.js";

export const profileApi = {
  path: () => "/profile",
  get: () => api("/profile"),
  update: (body) => api("/profile", { method: "PUT", body }),
};
