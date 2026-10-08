import { useMerchantDetail } from "../../../shared/hooks/useMerchantDetail.js";
import { Page } from "../../layout/Page.jsx";
import { Resource } from "../../feedback/Resource.jsx";
import { MerchantHeader } from "./MerchantHeader.jsx";
import { Button } from "../../ui/Button.jsx";
import { Plus } from "lucide-react";
import { Empty } from "../../feedback/Empty.jsx";

export function MerchantDetail({
  type,
  slug,
  resourceApi,
  extrasPath,
  chatApi,
  reviewsApi,
  ProductCard,
  AppointmentEditor,
  ReviewCard,
  ReviewEditor,
}) {
  const {
    item,
    state,
    mechanic,
    user,
    setAppointment,
    act,
    extras,
    setReview,
    confirm,
    toast,
    appointment,
    review,
  } = useMerchantDetail({ type, slug, resourceApi, extrasPath });
  return (
    <Page
      title={item?.businessName || item?.storeName || "Biznes profili"}
      description={item?.address}
    >
      <Resource state={state} skeleton="detail">
        {item && (
          <>
            <MerchantHeader
              item={item}
              mechanic={mechanic}
              user={user}
              setAppointment={setAppointment}
              act={act}
              chatApi={chatApi}
            />
            <div className="section-heading">
              <h2>{mechanic ? "Müştəri rəyləri" : "Mağazanın məhsulları"}</h2>
              {mechanic &&
                item.userId !== user.id &&
                !extras.loading &&
                !extras.error &&
                !extras.data?.some((value) => value.userId === user.id) && (
                  <Button variant="secondary" onClick={() => setReview({})}>
                    <Plus size={15} /> Rəy yaz
                  </Button>
                )}
            </div>
            <Resource state={extras} skeleton={mechanic ? "list" : "cards"}>
              {extras.data?.length ? (
                mechanic ? (
                  <div className="review-grid">
                    {extras.data.map((value) => (
                      <ReviewCard
                        key={value.id}
                        value={value}
                        user={user}
                        setReview={setReview}
                        confirm={confirm}
                        reviewsApi={reviewsApi}
                        extras={extras}
                        state={state}
                        toast={toast}
                      />
                    ))}
                  </div>
                ) : (
                  <div className="catalog-grid">
                    {extras.data.map((product) => (
                      <ProductCard product={product} key={product.id} />
                    ))}
                  </div>
                )
              ) : (
                <Empty
                  title={
                    mechanic ? "İlk rəyi siz yazın" : "Hazırda məhsul yoxdur"
                  }
                  description={
                    mechanic
                      ? "Təcrübənizi bölüşərək başqalarına kömək edin."
                      : "Mağazanın əlavə etdiyi məhsullar burada görünəcək."
                  }
                />
              )}
            </Resource>
            {appointment && (
              <AppointmentEditor
                mechanicId={item.id}
                onClose={() => setAppointment(false)}
                onDone={() =>
                  toast(
                    "Görüş yaradıldı. Görüşlərim bölməsində izləyə bilərsiniz.",
                  )
                }
              />
            )}
            {review && (
              <ReviewEditor
                review={review}
                setReview={setReview}
                reviewsApi={reviewsApi}
                item={item}
                extras={extras}
                state={state}
                toast={toast}
              />
            )}
          </>
        )}
      </Resource>
    </Page>
  );
}
