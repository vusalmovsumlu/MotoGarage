import { useState } from "react";
import { CalendarDays, ClipboardList, Plus } from "lucide-react";
import { api } from "../lib/api";
import { useResource } from "../lib/hooks";
import {
  appointmentTransitions,
  date,
  labels,
  money,
  orderTransitions,
} from "../lib/format";
import { Badge, Button, Empty, Page, Resource } from "../components/ui";
import Editor from "../components/Editor";
import { useApp } from "../context";

export function AppointmentEditor({ mechanicId, onClose, onDone }) {
  const vehicles = useResource("/Vehicle");
  const mechanics = useResource(mechanicId ? null : "/mechanics");
  return (
    <Resource state={vehicles} skeleton="form">
      {!vehicles.data?.length ? (
        <div className="card">
          <Empty
            title="Görüş üçün nəqliyyat lazımdır"
            description="Əvvəl qarajınıza nəqliyyat əlavə edin."
            action={
              <>
                <a href="#/vehicles" className="button primary">
                  Qaraja keç
                </a>
                <Button variant="ghost" onClick={onClose}>
                  Bağla
                </Button>
              </>
            }
          />
        </div>
      ) : (
        <Resource state={mechanics} skeleton="compact">
          {!mechanicId && !mechanics.data?.length ? (
            <Empty
              title="Hazırda aktiv usta yoxdur"
              action={<Button onClick={onClose}>Bağla</Button>}
            />
          ) : (
            <Editor
              title="Servis görüşü planlaşdırın"
              fields={[
                {
                  key: "vehicleId",
                  label: "Nəqliyyat vasitəsi",
                  options: vehicles.data.map((item) => ({
                    value: String(item.id),
                    label: `${item.brand} ${item.model} · ${item.plateNumber}`,
                  })),
                  required: true,
                  default: String(vehicles.data[0].id),
                },
                ...(!mechanicId
                  ? [
                      {
                        key: "mechanicId",
                        label: "Usta",
                        options: mechanics.data.map((item) => ({
                          value: String(item.id),
                          label: item.businessName,
                        })),
                        required: true,
                        default: String(mechanics.data[0].id),
                      },
                    ]
                  : []),
                {
                  key: "appointmentDate",
                  label: "Görüş tarixi və saatı",
                  type: "datetime-local",
                  required: true,
                  hint: "Saat bu cihazın yerli saat qurşağı ilə daxil edilir.",
                },
                {
                  key: "description",
                  label: "Görüləcək iş haqqında",
                  type: "textarea",
                  wide: true,
                  maxLength: 500,
                },
              ]}
              onClose={onClose}
              onSave={async (values) => {
                if (new Date(values.appointmentDate) <= new Date())
                  throw new Error("Gələcək tarix və saat seçin.");
                await api("/appointments", {
                  method: "POST",
                  body: {
                    ...values,
                    vehicleId: Number(values.vehicleId),
                    mechanicId: Number(mechanicId || values.mechanicId),
                  },
                });
                onDone();
                onClose();
              }}
            />
          )}
        </Resource>
      )}
    </Resource>
  );
}
export function WorkList({ type, incoming }) {
  const orders = type === "orders";
  const state = useResource(
    orders
      ? `/orders/${incoming ? "store" : "my"}`
      : `/appointments/${incoming ? "mechanic" : "my"}`,
  );
  const [filter, setFilter] = useState("all");
  const [create, setCreate] = useState(false);
  const [pending, setPending] = useState(null);
  const { act, confirm, toast } = useApp();
  const activeStatuses = orders
    ? ["Pending", "Confirmed", "Preparing", "Shipped"]
    : ["Pending", "Accepted"];
  const items = (state.data || []).filter(
    (item) =>
      filter === "all" ||
      (filter === "active"
        ? activeStatuses.includes(item.status)
        : !activeStatuses.includes(item.status)),
  );
  const title = orders
    ? incoming
      ? "Gələn sifarişlər"
      : "Sifarişlərim"
    : incoming
      ? "Gələn görüşlər"
      : "Görüşlərim";
  async function transition(item, status) {
    setPending(item.id);
    await act(async () => {
      await api(`/${type}/${item.id}/status`, {
        method: "PATCH",
        body: { status },
      });
      state.reload();
    }, "Status yeniləndi.");
    setPending(null);
  }
  return (
    <Page
      title={title}
      description={
        orders
          ? "Sifarişin ilk addımından tamamlanmasına qədər."
          : "Planınızı görün, görüşləri rahat idarə edin."
      }
      actions={
        !orders &&
        !incoming && (
          <Button onClick={() => setCreate(true)}>
            <Plus size={16} /> Görüş yarat
          </Button>
        )
      }
    >
      <div className="tabs" aria-label="Status üzrə filter">
        {[
          ["all", "Hamısı"],
          ["active", "Aktiv"],
          ["finished", "Bitmiş"],
        ].map(([key, name]) => (
          <button
            key={key}
            className={filter === key ? "active" : ""}
            aria-pressed={filter === key}
            onClick={() => setFilter(key)}
          >
            {name}
          </button>
        ))}
      </div>
      <Resource state={state}>
        {items.length ? (
          <div className="work-list">
            {items.map((item) => (
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
                        : `${date(item.appointmentDate, true)} · Nəqliyyat #${item.vehicleId} · ${incoming ? "Müştəri" : "Usta"} #${incoming ? item.userId : item.mechanicId}`}
                    </p>
                  </div>
                  <Badge value={item.status} />
                </div>
                {orders && (
                  <div className="order-items">
                    {item.items?.map((product) => (
                      <div key={product.id}>
                        <span>
                          {product.productName ||
                            `Məhsul #${product.productId}`}{" "}
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
                    (orders ? orderTransitions : appointmentTransitions)[
                      item.status
                    ]?.map((status) => (
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
                                  await api(`/${type}/${item.id}/status`, {
                                    method: "PATCH",
                                    body: { status },
                                  });
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
                              await api(`/${type}/${item.id}/cancel`, {
                                method: "PATCH",
                              });
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
            ))}
          </div>
        ) : (
          <Empty
            icon={orders ? ClipboardList : CalendarDays}
            title="Bu siyahıda heç nə yoxdur"
            description={
              incoming
                ? "Yeni müraciətlər burada görünəcək."
                : "Yeni sifariş və görüşləriniz burada görünəcək."
            }
          />
        )}
      </Resource>
      {create && (
        <AppointmentEditor
          onClose={() => setCreate(false)}
          onDone={() => {
            state.reload();
            toast("Görüş yaradıldı.");
          }}
        />
      )}
    </Page>
  );
}
