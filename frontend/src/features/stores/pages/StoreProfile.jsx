import { BusinessProfile } from "../../../components/common/merchants/BusinessProfile.jsx";
import { storesApi } from "../api/storesApi.js";
export function StoreProfile() {
  return <BusinessProfile resourceApi={storesApi} />;
}
