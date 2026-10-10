import { date, money } from "../../../shared/utils/format.js";
import { Badge } from "../../ui/Badge.jsx";
import { Button } from "../../ui/Button.jsx";
import { labels } from "../../../shared/constants/labels.js";

export function WorkCard({
  item,
  orders,
  incoming,
  transitions,
  pending,
  confirm,
  resourceApi,
  state,
  toast,
  transition,
}) {
  return (
    <article className="card work-card" key={item.id}>
      <div className="work-heading">
        <div>
          <span className="meta">
            {orders ? "SİFARİŞ" : "GÖRÜŞ"} #{item.id}
          </span>
          <h2>
            {orders
              ? `${incoming ? "Müştəri" : "Mağaza"} #${incoming ? item.userId : item.storeId}`
              : item.description || "Servis görüşü"}
          </h2>
          <p className="meta">
            {orders
              ? date(item.createdAt, true)
              : `${date(item.appointmentDate, true)} · ${item.vehicleName || "Nəqliyyat məlumatı yoxdur"} · ${incoming ? item.customerName || "Müştəri adı yoxdur" : item.mechanicName || "Usta adı yoxdur"}`}
          </p>
        </div>
        <Badge value={item.status} />
      </div>
      {orders && (
        <div className="order-items">
          {item.items?.map((product) => (
            <div key={product.id}>
              <span>
                {product.productName || `Məhsul #${product.productId}`}{" "}
                <small>× {product.quantity}</small>
              </span>
              <strong>{money(product.totalPrice)}</strong>
            </div>
          ))}
          <div className="order-total">
            <span>Ümumi məbləğ</span>
            <strong>{money(item.totalAmount)}</strong>
          </div>
        </div>
      )}
      <div className="work-actions">
        {incoming &&
          transitions[item.status]?.map((status) => (
            <Button
              key={status}
              variant={
                ["Cancelled", "Rejected"].includes(status)
                  ? "secondary"
                  : "primary"
              }
              busy={pending === item.id}
              onClick={() =>
                ["Cancelled", "Rejected"].includes(status)
                  ? confirm({
                      title: `${orders ? "Sifariş" : "Görüş"} ${labels[status].toLowerCase()}?`,
                      description: orders
                        ? "Sifariş ləğv ediləndə stok yalnız bir dəfə geri qaytarılır."
                        : "Bu statusdan geri keçid mümkün olmayacaq.",
                      action: async () => {
                        await resourceApi.updateStatus(item.id, status);
                        state.reload();
                        toast("Status yeniləndi.");
                      },
                    })
                  : transition(item, status)
              }
            >
              {labels[status]}
            </Button>
          ))}
        {!incoming &&
          ["Pending", orders ? "Confirmed" : "Accepted"].includes(
            item.status,
          ) && (
            <Button
              variant="secondary"
              onClick={() =>
                confirm({
                  title: `${orders ? "Sifarişi" : "Görüşü"} ləğv etmək istəyirsiniz?`,
                  description: "Bu əməliyyat geri qaytarıla bilməz.",
                  action: async () => {
                    await resourceApi.cancel(item.id);
                    state.reload();
                    toast("Ləğv edildi.");
                  },
                })
              }
            >
              Ləğv et
            </Button>
          )}
      </div>
    </article>
  );
}
