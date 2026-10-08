import { Records } from "../../../components/common/records/Records.jsx";
import { garageApi } from "../../garage/api/garageApi.js";
import { recordApi } from "../api/serviceRecordsApi.js";
import { recordDefinition } from "../utils/recordDefinition.js";

const definition = { ...recordDefinition, recordApi };
export function ServiceRecords({ route }) {
  return (
    <Records
      kind="service"
      route={route}
      definition={definition}
      vehicleApi={garageApi}
    />
  );
}
