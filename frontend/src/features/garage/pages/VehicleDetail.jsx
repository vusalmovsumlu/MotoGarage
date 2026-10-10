import { useResource } from "../../../shared/hooks/useResource.js";
import { garageApi } from "../api/garageApi.js";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Thumbnail } from "../../../components/common/Thumbnail.jsx";
import {
  CarFront,
  Gauge,
  Fuel,
  Wrench,
  Wallet,
  Bell,
  ChevronRight,
} from "lucide-react";
import { Badge } from "../../../components/ui/Badge.jsx";
import { labels } from "../../../shared/constants/labels.js";
import { number } from "../../../shared/utils/format.js";
import { useEffect, useState } from "react";
import { Button } from "../../../components/ui/Button.jsx";
import { VehicleHistoryExport } from "../components/VehicleHistoryExport.jsx";
import { vehiclePath } from "../../../shared/utils/vehicleRoutes.js";
import { replaceRoute } from "../../../app/router/useRoute.js";

export function VehicleDetail({ id, slug }) {
  const [exportOpen, setExportOpen] = useState(false);
  const state = useResource(
    slug ? garageApi.slugPath(slug) : garageApi.detailPath(id),
  );
  const vehicle = state.data || {};
  useEffect(() => {
    if (!slug && state.data?.slug) {
      replaceRoute(vehiclePath(state.data));
    }
  }, [slug, state.data]);
  return (
    <Page
      title={
        state.data ? `${vehicle.brand} ${vehicle.model}` : "Nəqliyyat vasitəsi"
      }
      description="Qarajınızdakı bütün qeydlərə bir yerdən keçin."
      actions={
        state.data?.slug && (
          <Button onClick={() => setExportOpen(true)}>Tarixçəni paylaş</Button>
        )
      }
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
              href={`#${vehiclePath(vehicle, key)}`}
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
      {exportOpen && state.data && (
        <VehicleHistoryExport
          vehicle={vehicle}
          onClose={() => setExportOpen(false)}
        />
      )}
    </Page>
  );
}
