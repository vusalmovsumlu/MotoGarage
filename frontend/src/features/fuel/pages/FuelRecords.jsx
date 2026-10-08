import { Records } from "../../../components/common/records/Records.jsx";
import { garageApi } from "../../garage/api/garageApi.js";
import { recordApi } from "../api/fuelApi.js";
import { recordDefinition } from "../utils/recordDefinition.js";
import { FuelStats } from "../components/FuelStats.jsx";
const definition = { ...recordDefinition, recordApi };
export function FuelRecords({ route }) {
  return (
    <Records
      kind="fuel"
      route={route}
      definition={definition}
      vehicleApi={garageApi}
      FuelStats={FuelStats}
    />
  );
}
