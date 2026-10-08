import { useVehicleRecords } from "../../../shared/hooks/useVehicleRecords.js";
import { Page } from "../../layout/Page.jsx";
import { Button } from "../../ui/Button.jsx";
import { Plus, CarFront } from "lucide-react";
import { Resource } from "../../feedback/Resource.jsx";
import { RecordFilters } from "./RecordFilters.jsx";
import { RecordRow } from "./RecordRow.jsx";
import { Empty } from "../../feedback/Empty.jsx";
import { RecordEditor } from "./RecordEditor.jsx";

export function Records({ kind, route, definition, vehicleApi, FuelStats }) {
  const {
    activeVehicle,
    setEdit,
    vehicles,
    vehicleId,
    setVehicleId,
    startDate,
    endDate,
    setFilterError,
    setRange,
    setStart,
    setEnd,
    filterError,
    stats,
    state,
    records,
    Icon,
    act,
    recordApi,
    reload,
    confirm,
    toast,
    edit,
  } = useVehicleRecords({ kind, route, definition, vehicleApi });
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
            <RecordFilters
              vehicleId={vehicleId}
              setVehicleId={setVehicleId}
              vehicles={vehicles}
              kind={kind}
              startDate={startDate}
              endDate={endDate}
              setFilterError={setFilterError}
              setRange={setRange}
              setStart={setStart}
              setEnd={setEnd}
            />
            {filterError && (
              <p role="alert" className="form-error">
                {filterError}
              </p>
            )}
            {kind === "fuel" && (
              <Resource state={stats} skeleton="stats">
                <FuelStats stats={stats} />
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
                      <RecordRow
                        key={item.id}
                        item={item}
                        Icon={Icon}
                        kind={kind}
                        definition={definition}
                        act={act}
                        recordApi={recordApi}
                        reload={reload}
                        setEdit={setEdit}
                        confirm={confirm}
                        toast={toast}
                      />
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
        <RecordEditor
          definition={definition}
          edit={edit}
          setEdit={setEdit}
          recordApi={recordApi}
          vehicleId={vehicleId}
          reload={reload}
          toast={toast}
        />
      )}
    </Page>
  );
}
