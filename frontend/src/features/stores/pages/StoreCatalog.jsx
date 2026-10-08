import { Catalog } from "../../../components/common/catalog/Catalog.jsx";
import { storesApi } from "../api/storesApi.js";
export function StoreCatalog() {
  return <Catalog type="stores" resourceApi={storesApi} />;
}
