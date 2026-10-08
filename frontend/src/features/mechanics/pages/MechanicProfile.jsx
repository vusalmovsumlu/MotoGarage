import { BusinessProfile } from "../../../components/common/merchants/BusinessProfile.jsx";
import { mechanicsApi } from "../api/mechanicsApi.js";
export function MechanicProfile() {
  return <BusinessProfile resourceApi={mechanicsApi} />;
}
