import { createVehicleRecordApi } from "../../../shared/api/createVehicleRecordApi.js";
import { query } from "../../../shared/utils/query.js";
export const recordApi = createVehicleRecordApi("/fuel-records", {
  statsPath: (vehicleId, range) =>
    `/fuel-records/stats${query({ vehicleId, ...range })}`,
});
