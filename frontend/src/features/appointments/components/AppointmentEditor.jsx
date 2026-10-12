import { useResource } from "../../../shared/hooks/useResource.js";
import { garageApi } from "../../garage/api/garageApi.js";
import { mechanicsApi } from "../../mechanics/api/mechanicsApi.js";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import Editor from "../../../components/common/Editor.jsx";
import { AvailabilityPicker } from "./AvailabilityPicker.jsx";
import { appointmentsApi } from "../api/appointmentsApi.js";

export function AppointmentEditor({ mechanicId, onClose, onDone }) {
  const vehicles = useResource(garageApi.listPath());
  const mechanics = useResource(mechanicId ? null : mechanicsApi.listPath());
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
                        reset: ["appointmentDate"],
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
                  type: "slot",
                  required: true,
                  default: "",
                  wide: true,
                  render: ({ values, value, onChange, id }) => (
                    <AvailabilityPicker
                      key={mechanicId || values.mechanicId}
                      id={id}
                      mechanicId={mechanicId || values.mechanicId}
                      value={value}
                      onChange={onChange}
                    />
                  ),
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
                if (
                  !values.appointmentDate ||
                  new Date(values.appointmentDate) <= new Date()
                )
                  throw new Error("Gələcək tarix və saat seçin.");
                await appointmentsApi.create({
                  ...values,
                  vehicleId: Number(values.vehicleId),
                  mechanicId: Number(mechanicId || values.mechanicId),
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
