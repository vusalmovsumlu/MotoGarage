import { api } from "./client.js";
export function createVehicleRecordApi(endpoint, options = {}) {
  return {
    listPath:
      options.listPath || ((vehicleId) => `${endpoint}/vehicle/${vehicleId}`),
    statsPath: options.statsPath || (() => null),
    save: (edit, vehicleId, values) =>
      api(edit.id ? `${endpoint}/${edit.id}` : endpoint, {
        method: edit.id ? "PUT" : "POST",
        body: {
          ...values,
          ...(!edit.id ? { vehicleId: Number(vehicleId) } : {}),
        },
      }),
    remove: (id) => api(`${endpoint}/${id}`, { method: "DELETE" }),
    ...(options.completePath
      ? { complete: (id) => api(options.completePath(id), { method: "PATCH" }) }
      : {}),
  };
}
