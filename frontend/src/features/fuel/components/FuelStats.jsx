import { Stat } from "../../../components/common/Stat.jsx";
import { money, number } from "../../../shared/utils/format.js";

export function FuelStats({ stats }) {
  return (
    <div className="stats-grid">
      <Stat label="Yanacaq xərci" value={money(stats.data?.totalFuelCost)} />
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
  );
}
