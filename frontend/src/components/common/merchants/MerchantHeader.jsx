import { Thumbnail } from "../Thumbnail.jsx";
import { Wrench, Store, Star, Phone, MessageCircle } from "lucide-react";
import { number, money } from "../../../shared/utils/format.js";
import { Button } from "../../ui/Button.jsx";
import { navigate } from "../../../app/router/useRoute.js";

export function MerchantHeader({
  item,
  mechanic,
  user,
  setAppointment,
  act,
  chatApi,
  preview = false,
}) {
  return (
    <div className="card merchant-detail">
      <Thumbnail
        src={item.imageUrl}
        icon={mechanic ? Wrench : Store}
        alt={item.businessName || item.storeName}
      />
      <div>
        <p className="eyebrow">
          {mechanic ? "USTA PROFİLİ" : "MAĞAZA PROFİLİ"}
        </p>
        <h2>{item.businessName || item.storeName}</h2>
        <p className="muted">{item.description}</p>
        <div className="merchant-contact">
          <span>
            <Star size={15} />
            {number(item.rating)} / 5
          </span>
          <a href={`tel:${item.phoneNumber}`}>
            <Phone size={15} />
            {item.phoneNumber}
          </a>
          {item.hourlyRate != null && (
            <strong>{money(item.hourlyRate)} / saat</strong>
          )}
        </div>
        {!preview && item.userId !== user.id && (
          <div className="actions">
            {mechanic && (
              <Button onClick={() => setAppointment(true)}>
                Görüş planlaşdır
              </Button>
            )}
            <Button
              variant="secondary"
              onClick={() =>
                act(async () => {
                  const result = await chatApi.start(
                    mechanic ? "mechanic" : "store",
                    item.id,
                  );
                  navigate(`/chat/${result.id}`);
                })
              }
            >
              <MessageCircle size={16} /> Mesaj yaz
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
