import { createVehicleRecordApi } from "../../../shared/api/createVehicleRecordApi.js";

export const recordApi = createVehicleRecordApi("/reminders", {
  completePath: (id) => `/reminders/${id}/complete`,
});
