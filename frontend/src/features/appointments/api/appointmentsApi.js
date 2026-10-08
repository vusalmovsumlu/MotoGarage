import { api } from "../../../shared/api/client.js";
import { createWorkflowApi } from "../../../shared/api/createWorkflowApi.js";
export const appointmentsApi = {
  ...createWorkflowApi("/appointments", "mechanic"),
  create: (body) => api("/appointments", { method: "POST", body }),
};
