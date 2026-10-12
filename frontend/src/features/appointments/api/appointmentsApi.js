import { api } from "../../../shared/api/client.js";
import { createWorkflowApi } from "../../../shared/api/createWorkflowApi.js";
export const appointmentsApi = {
  ...createWorkflowApi("/appointments", "mechanic"),
  hoursPath: () => "/appointments/working-hours",
  calendarPath: (from, to) => `/appointments/calendar?from=${from}&to=${to}`,
  availabilityPath: (mechanicId, day, duration = 60) =>
    `/appointments/availability?mechanicId=${Number(mechanicId)}&date=${day}&durationMinutes=${duration}`,
  saveHours: (body) =>
    api("/appointments/working-hours", { method: "PUT", body }),
  propose: (id, body) =>
    api(`/appointments/${id}/reschedule`, { method: "POST", body }),
  decideReschedule: (id, body) =>
    api(`/appointments/${id}/reschedule/decision`, { method: "POST", body }),
  reportPath: (id) => `/appointments/${id}/report`,
  submitReport: (id, body) =>
    api(`/appointments/${id}/report`, { method: "PUT", body }),
  decideReport: (id, body) =>
    api(`/appointments/${id}/report/decision`, { method: "POST", body }),
  create: (body) => api("/appointments", { method: "POST", body }),
};
