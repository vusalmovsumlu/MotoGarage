import { api } from "../../../shared/api/client.js";
import { createWorkflowApi } from "../../../shared/api/createWorkflowApi.js";
export const ordersApi = {
  ...createWorkflowApi("/orders", "store"),
  create: (body) => api("/orders", { method: "POST", body }),
};
