import { api } from "./client.js";
export function createWorkflowApi(basePath, incomingPath) {
  return {
    listPath: (incoming) => `${basePath}/${incoming ? incomingPath : "my"}`,
    updateStatus: (id, status) =>
      api(`${basePath}/${id}/status`, { method: "PATCH", body: { status } }),
    cancel: (id) => api(`${basePath}/${id}/cancel`, { method: "PATCH" }),
  };
}
