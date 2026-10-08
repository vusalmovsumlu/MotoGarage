import { MerchantDetail } from "../../../components/common/merchants/MerchantDetail.jsx";
import { mechanicsApi } from "../api/mechanicsApi.js";
import { chatApi } from "../../chat/api/chatApi.js";
import { reviewsApi } from "../../reviews/api/reviewsApi.js";
import { ReviewCard } from "../../reviews/components/ReviewCard.jsx";
import { ReviewEditor } from "../../reviews/components/ReviewEditor.jsx";
import { AppointmentEditor } from "../../appointments/components/AppointmentEditor.jsx";
const extrasPath = (item) => reviewsApi.mechanicReviewsPath(item.id);
export function MechanicDetail({ slug }) {
  return (
    <MerchantDetail
      type="mechanics"
      slug={slug}
      resourceApi={mechanicsApi}
      extrasPath={extrasPath}
      chatApi={chatApi}
      reviewsApi={reviewsApi}
      ReviewCard={ReviewCard}
      ReviewEditor={ReviewEditor}
      AppointmentEditor={AppointmentEditor}
    />
  );
}
