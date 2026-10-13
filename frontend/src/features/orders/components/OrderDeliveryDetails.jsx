import { MapPin, Phone } from "lucide-react";

export function OrderDeliveryDetails({ item }) {
  return (
    <section className="order-delivery" aria-label="Çatdırılma məlumatları">
      <h3>
        <MapPin size={17} /> Çatdırılma ünvanı
      </h3>
      {item.deliveryAddress ? (
        <>
          <strong>{item.recipientName}</strong>
          <p>
            {item.deliveryCity} · {item.deliveryAddress}
          </p>
          {item.deliveryPhone && (
            <p>
              <Phone size={14} />{" "}
              <a href={`tel:${item.deliveryPhone.replace(/[^+0-9]/g, "")}`}>
                {item.deliveryPhone}
              </a>
            </p>
          )}
          {item.deliveryNote && (
            <p className="meta">Qeyd: {item.deliveryNote}</p>
          )}
        </>
      ) : (
        <p className="meta">
          Bu əvvəlki sifarişdə çatdırılma məlumatı saxlanılmayıb.
        </p>
      )}
    </section>
  );
}
