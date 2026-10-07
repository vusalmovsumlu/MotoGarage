import { useState } from "react";
import {
  Bell,
  CarFront,
  ChevronRight,
  Fuel,
  Gauge,
  Pencil,
  Plus,
  Trash2,
  Wallet,
  Wrench,
} from "lucide-react";
import { api, query } from "../lib/api";
import { useResource } from "../lib/hooks";
import { date, labels, money, number } from "../lib/format";
import { recordDefinitions, vehicleFields } from "../lib/fields";
import {
  Badge,
  Button,
  Empty,
  IconButton,
  Page,
  Resource,
  Stat,
  Thumbnail,
} from "../components/ui";
import Editor from "../components/Editor";
import { useApp } from "../context";

export function Vehicles() {
  const state = useResource("/Vehicle");
  const [edit, setEdit] = useState(null);
  const { toast, confirm } = useApp();
  return (
    <Page
      title="Qarajım"
      description="Hər nəqliyyat vasitəsinin öz hekayəsi var. Burada davam etdirin."
      actions={
        <Button onClick={() => setEdit({})}>
          <Plus size={17} /> Nəqliyyat əlavə et
        </Button>
      }
    >
      <Resource state={state} skeleton="cards">
        {state.data?.length ? (
          <div className="vehicle-grid">
            {state.data.map((vehicle) => (
              <article className="card vehicle-card" key={vehicle.id}>
                <a href={`#/vehicles/${vehicle.id}`} className="vehicle-image">
                  <Thumbnail
                    src={vehicle.imageUrl}
                    icon={CarFront}
                    alt={`${vehicle.brand} ${vehicle.model}`}
                  />
                </a>
                <div className="vehicle-card-heading">
                  <div>
                    <span className="meta">
                      {labels[vehicle.vehicleType]} · {vehicle.year}
                    </span>
                    <h2>
                      <a href={`#/vehicles/${vehicle.id}`}>
                        {vehicle.brand} {vehicle.model}
                      </a>
                    </h2>
                  </div>
                  <IconButton
                    label="Nəqliyyatı dəyiş"
                    onClick={() => setEdit(vehicle)}
                  >
                    <Pencil size={16} />
                  </IconButton>
                </div>
                <div className="vehicle-meta">
                  <span className="plate">{vehicle.plateNumber}</span>
                  <span>
                    <Gauge size={15} />
                    {number(vehicle.currentKm)} km
                  </span>
                </div>
                <div className="card-footer">
                  <a className="text-link" href={`#/vehicles/${vehicle.id}`}>
                    Tarixçəyə bax <ChevronRight size={15} />
                  </a>
                  <IconButton
                    label="Nəqliyyatı sil"
                    onClick={() =>
                      confirm({
                        title: "Nəqliyyatı silmək istəyirsiniz?",
                        description:
                          "Bağlı yanacaq, servis, xərc və xatırlatma qeydləri də silinəcək. Görüşü olan nəqliyyat silinə bilməz.",
                        action: async () => {
                          await api(`/Vehicle/${vehicle.id}`, {
                            method: "DELETE",
                          });
                          state.reload();
                          toast("Nəqliyyat silindi.");
                        },
                      })
                    }
                  >
                    <Trash2 size={15} />
                  </IconButton>
                </div>
              </article>
            ))}
          </div>
        ) : (
          <Empty
            icon={CarFront}
            title="Qarajınız sizi gözləyir"
            description="Avtomobilinizi və ya motosikletinizi əlavə edib tarixçəsini izləyin."
            action={
              <Button onClick={() => setEdit({})}>
                <Plus size={16} /> İlk nəqliyyatı əlavə et
              </Button>
            }
          />
        )}
      </Resource>
      {edit && (
        <Editor
          title={edit.id ? "Nəqliyyatı dəyiş" : "Qarajınıza əlavə edin"}
          fields={vehicleFields.map((field) =>
            field.key === "currentKm" && edit.id
              ? {
                  ...field,
                  min: edit.currentKm,
                  hint: "Cari odometr azaldıla bilməz.",
                }
              : field,
          )}
          initial={edit}
          image
          onClose={() => setEdit(null)}
          onSave={async (body) => {
            await api(edit.id ? `/Vehicle/${edit.id}` : "/Vehicle", {
              method: edit.id ? "PUT" : "POST",
              body,
            });
            setEdit(null);
            state.reload();
            toast("Nəqliyyat yadda saxlanıldı.");
          }}
        />
      )}
    </Page>
  );
}
export function VehicleDetail({ id }) {
  const state = useResource(`/Vehicle/${id}`);
  const vehicle = state.data || {};
  return (
    <Page
      title={
        state.data ? `${vehicle.brand} ${vehicle.model}` : "Nəqliyyat vasitəsi"
      }
      description="Qarajınızdakı bütün qeydlərə bir yerdən keçin."
    >
      <Resource state={state} skeleton="detail">
        <div className="card vehicle-detail">
          <Thumbnail
            src={vehicle.imageUrl}
            icon={CarFront}
            alt={`${vehicle.brand} ${vehicle.model}`}
          />
          <div>
            <Badge value={vehicle.vehicleType} />
            <h2>
              {vehicle.brand} {vehicle.model}
            </h2>
            <span className="plate">{vehicle.plateNumber}</span>
            <p className="muted">
              {vehicle.year} · {vehicle.engine} · {labels[vehicle.fuelType]}
            </p>
            <p className="meta">VIN: {vehicle.vin}</p>
          </div>
          <div className="odometer">
            <Gauge size={22} />
            <strong>{number(vehicle.currentKm)}</strong>
            <span className="meta">cari odometr · km</span>
          </div>
        </div>
        <div className="record-destinations">
          {[
            ["fuel", Fuel, "Yanacaq", "Doldurma və sərfiyyat"],
            ["service", Wrench, "Servis tarixçəsi", "Görülən işlər və xərclər"],
            ["expenses", Wallet, "Xərclər", "Büdcə və kateqoriyalar"],
            ["reminders", Bell, "Xatırlatmalar", "Növbəti texniki qulluq"],
          ].map(([key, Icon, title, desc]) => (
            <a
              className="card record-destination"
              href={`#/records/${key}?vehicle=${id}`}
              key={key}
            >
              <Icon size={23} />
              <div>
                <strong>{title}</strong>
                <span className="meta">{desc}</span>
              </div>
              <ChevronRight size={18} />
            </a>
          ))}
        </div>
      </Resource>
    </Page>
  );
}
export function Records({ kind, route }) {
  const definition = recordDefinitions[kind];
  const vehicles = useResource("/Vehicle");
  const [selectedVehicleId, setVehicleId] = useState(
    new URLSearchParams(route.split("?")[1]).get("vehicle") || "",
  );
  const vehicleId =
    selectedVehicleId ||
    (vehicles.data?.length ? String(vehicles.data[0].id) : "");
  const [startDate, setStart] = useState("");
  const [endDate, setEnd] = useState("");
  const [range, setRange] = useState({});
  const [filterError, setFilterError] = useState("");
  const [edit, setEdit] = useState(null);
  const { toast, confirm, act } = useApp();
  const state = useResource(
    vehicleId
      ? kind === "expenses"
        ? `/expenses${query({ vehicleId, ...range })}`
        : `${definition.endpoint}/vehicle/${vehicleId}`
      : null,
  );
  const stats = useResource(
    kind === "fuel" && vehicleId
      ? `/fuel-records/stats${query({ vehicleId, ...range })}`
      : null,
  );
  const records = (
    Array.isArray(state.data) ? state.data : state.data?.expenses || []
  ).filter(
    (item) =>
      kind !== "fuel" ||
      !range.startDate ||
      (item.fuelDate.slice(0, 10) >= range.startDate &&
        item.fuelDate.slice(0, 10) <= range.endDate),
  );
  const reload = () => {
    state.reload();
    stats.reload();
    vehicles.reload();
  };
  const activeVehicle = vehicles.data?.find(
    (item) => item.id === Number(vehicleId),
  );
  const Icon = {
    fuel: Fuel,
    service: Wrench,
    expenses: Wallet,
    reminders: Bell,
  }[kind];
  return (
    <Page
      title={definition.title}
      description="Qarajınızın detalları, günbəgün."
      actions={
        <Button
          disabled={!activeVehicle}
          onClick={() => setEdit({ kilometer: activeVehicle.currentKm })}
        >
          <Plus size={16} /> Qeyd əlavə et
        </Button>
      }
    >
      <Resource state={vehicles} skeleton="form">
        {vehicles.data?.length ? (
          <>
            <div className="card record-filters">
              <label className="field">
                <span>Nəqliyyat vasitəsi</span>
                <select
                  aria-label="Nəqliyyat vasitəsi"
                  value={vehicleId}
                  onChange={(event) => setVehicleId(event.target.value)}
                >
                  {vehicles.data.map((vehicle) => (
                    <option value={vehicle.id} key={vehicle.id}>
                      {vehicle.brand} {vehicle.model} · {vehicle.plateNumber}
                    </option>
                  ))}
                </select>
              </label>
              {["fuel", "expenses"].includes(kind) && (
                <form
                  className="date-filter"
                  onSubmit={(event) => {
                    event.preventDefault();
                    if (
                      Boolean(startDate) !== Boolean(endDate) ||
                      (startDate && startDate > endDate)
                    ) {
                      setFilterError(
                        "Hər iki tarixi düzgün ardıcıllıqla seçin.",
                      );
                      return;
                    }
                    setFilterError("");
                    setRange(startDate ? { startDate, endDate } : {});
                  }}
                >
                  <label className="field">
                    <span>Başlanğıc</span>
                    <input
                      aria-label="Başlanğıc tarixi"
                      type="date"
                      value={startDate}
                      onChange={(event) => setStart(event.target.value)}
                    />
                  </label>
                  <label className="field">
                    <span>Son gün daxil</span>
                    <input
                      aria-label="Son tarix"
                      type="date"
                      value={endDate}
                      onChange={(event) => setEnd(event.target.value)}
                    />
                  </label>
                  <Button variant="secondary" type="submit">
                    Filtrlə
                  </Button>
                  <Button
                    variant="ghost"
                    type="button"
                    onClick={() => {
                      setStart("");
                      setEnd("");
                      setRange({});
                      setFilterError("");
                    }}
                  >
                    Sıfırla
                  </Button>
                </form>
              )}
            </div>
            {filterError && (
              <p role="alert" className="form-error">
                {filterError}
              </p>
            )}
            {kind === "fuel" && (
              <Resource state={stats} skeleton="stats">
                <div className="stats-grid">
                  <Stat
                    label="Yanacaq xərci"
                    value={money(stats.data?.totalFuelCost)}
                  />
                  <Stat
                    label="Ümumi yanacaq"
                    value={`${number(stats.data?.totalLiters)} L`}
                  />
                  <Stat
                    label="Gedilən məsafə"
                    value={`${number(stats.data?.distanceKm)} km`}
                  />
                  <Stat
                    label="Orta sərfiyyat"
                    value={
                      stats.data?.recordCount >= 2 && stats.data?.distanceKm > 0
                        ? `${number(stats.data.averageConsumptionPer100Km)} L/100 km`
                        : "—"
                    }
                    note="Hesablama üçün ən azı 2 qeyd lazımdır."
                  />
                </div>
              </Resource>
            )}
            <Resource state={state}>
              {records.length ? (
                <div className="card rows-card">
                  {[...records]
                    .sort((a, b) =>
                      b[definition.dateKey].localeCompare(
                        a[definition.dateKey],
                      ),
                    )
                    .map((item) => (
                      <div className="record-row" key={item.id}>
                        <span className="record-icon">
                          <Icon size={19} />
                        </span>
                        <div className="record-info">
                          <strong>
                            {kind === "fuel"
                              ? `${number(item.liters)} litr · ${money(item.pricePerLiter)}/L`
                              : kind === "service"
                                ? item.serviceName
                                : kind === "expenses"
                                  ? item.description
                                  : item.title}
                          </strong>
                          <span className="meta">
                            {date(item[definition.dateKey], true)}
                            {item.kilometer != null &&
                              ` · ${number(item.kilometer)} km`}
                            {kind === "expenses" &&
                              ` · ${labels[item.expenseType]}`}
                            {item.reminderKm != null &&
                              ` · ${number(item.reminderKm)} km`}
                          </span>
                          {item.description && kind !== "expenses" && (
                            <span className="meta">{item.description}</span>
                          )}
                        </div>
                        <strong className="row-value">
                          {kind === "fuel" ? (
                            money(item.totalPrice)
                          ) : kind === "service" ? (
                            money(item.cost)
                          ) : kind === "expenses" ? (
                            money(item.amount)
                          ) : item.isCompleted ? (
                            <Badge value="Completed" />
                          ) : (
                            <Badge value="Pending" />
                          )}
                        </strong>
                        <div className="row-actions">
                          {kind === "reminders" && !item.isCompleted && (
                            <Button
                              variant="ghost"
                              onClick={() =>
                                act(async () => {
                                  await api(`/reminders/${item.id}/complete`, {
                                    method: "PATCH",
                                  });
                                  reload();
                                }, "Xatırlatma tamamlandı.")
                              }
                            >
                              Tamamla
                            </Button>
                          )}
                          <IconButton
                            label="Qeydi dəyiş"
                            onClick={() => setEdit(item)}
                          >
                            <Pencil size={16} />
                          </IconButton>
                          <IconButton
                            label="Qeydi sil"
                            onClick={() =>
                              confirm({
                                title: "Qeydi silmək istəyirsiniz?",
                                description:
                                  "Bu əməliyyat geri qaytarıla bilməz. Cari odometr azaldılmayacaq.",
                                action: async () => {
                                  await api(
                                    `${definition.endpoint}/${item.id}`,
                                    { method: "DELETE" },
                                  );
                                  reload();
                                  toast("Qeyd silindi.");
                                },
                              })
                            }
                          >
                            <Trash2 size={16} />
                          </IconButton>
                        </div>
                      </div>
                    ))}
                </div>
              ) : (
                <Empty
                  icon={Icon}
                  title="Bu aralıqda qeyd yoxdur"
                  description="Yeni qeyd əlavə edin və ya tarix filterini dəyişin."
                />
              )}
            </Resource>
          </>
        ) : (
          <Empty
            icon={CarFront}
            title="Əvvəl nəqliyyat əlavə edin"
            description="Qeydlər qarajınızdakı nəqliyyat vasitəsinə bağlıdır."
            action={
              <a className="button primary" href="#/vehicles">
                Qaraja keç
              </a>
            }
          />
        )}
      </Resource>
      {edit && (
        <Editor
          title={`${definition.singular} ${edit.id ? "dəyiş" : "əlavə et"}`}
          fields={definition.fields}
          initial={edit}
          onClose={() => setEdit(null)}
          onSave={async (values) => {
            await api(
              edit.id
                ? `${definition.endpoint}/${edit.id}`
                : definition.endpoint,
              {
                method: edit.id ? "PUT" : "POST",
                body: {
                  ...values,
                  ...(!edit.id ? { vehicleId: Number(vehicleId) } : {}),
                },
              },
            );
            setEdit(null);
            reload();
            toast("Qeyd yadda saxlanıldı.");
          }}
        />
      )}
    </Page>
  );
}
