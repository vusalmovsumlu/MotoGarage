import { Thumbnail } from "../../../components/common/Thumbnail.jsx";
import { CarFront, Pencil, Gauge, ChevronRight, Trash2 } from "lucide-react";
import { labels } from "../../../shared/constants/labels.js";
import { IconButton } from "../../../components/ui/IconButton.jsx";
import { number } from "../../../shared/utils/format.js";
import { garageApi } from "../api/garageApi.js";
import { vehiclePath } from "../../../shared/utils/vehicleRoutes.js";

export function VehicleCard({ vehicle, setEdit, confirm, state, toast }) {
  return (
    <article className="card vehicle-card" key={vehicle.id}>
      <a href={`#${vehiclePath(vehicle)}`} className="vehicle-image">
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
            <a href={`#${vehiclePath(vehicle)}`}>
              {vehicle.brand} {vehicle.model}
            </a>
          </h2>
        </div>
        <IconButton label="Nəqliyyatı dəyiş" onClick={() => setEdit(vehicle)}>
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
        <a className="text-link" href={`#${vehiclePath(vehicle)}`}>
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
                await garageApi.remove(vehicle.id);
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
  );
}
