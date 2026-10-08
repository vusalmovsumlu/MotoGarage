import { Catalog } from "../../../components/common/catalog/Catalog.jsx";
import { mechanicsApi } from "../api/mechanicsApi.js";
export function MechanicCatalog() {
  return <Catalog type="mechanics" resourceApi={mechanicsApi} />;
}
