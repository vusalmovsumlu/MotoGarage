import { Button } from "../../ui/Button.jsx";

export function RecordFilters({
  vehicleId,
  setVehicleId,
  vehicles,
  kind,
  startDate,
  endDate,
  setFilterError,
  setRange,
  setStart,
  setEnd,
}) {
  return (
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
              setFilterError("Hər iki tarixi düzgün ardıcıllıqla seçin.");
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
  );
}
