import { Records } from "../../../components/common/records/Records.jsx";
import { garageApi } from "../../garage/api/garageApi.js";
import { recordApi } from "../api/expensesApi.js";
import { recordDefinition } from "../utils/recordDefinition.js";

const definition = { ...recordDefinition, recordApi };
export function Expenses({ route }) {
  return (
    <Records
      kind="expenses"
      route={route}
      definition={definition}
      vehicleApi={garageApi}
    />
  );
}
