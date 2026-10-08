import { Records } from "../../../components/common/records/Records.jsx";
import { garageApi } from "../../garage/api/garageApi.js";
import { recordApi } from "../api/remindersApi.js";
import { recordDefinition } from "../utils/recordDefinition.js";

const definition = { ...recordDefinition, recordApi };
export function Reminders({ route }) {
  return (
    <Records
      kind="reminders"
      route={route}
      definition={definition}
      vehicleApi={garageApi}
    />
  );
}
