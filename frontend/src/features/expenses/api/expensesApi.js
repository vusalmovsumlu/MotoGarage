import { createVehicleRecordApi } from "../../../shared/api/createVehicleRecordApi.js";
import { query } from "../../../shared/utils/query.js";
export const recordApi = createVehicleRecordApi("/expenses", {
  listPath: (vehicleId, range) => `/expenses${query({ vehicleId, ...range })}`,
});
