import { useVehicles } from "../hooks/useVehicles.js";
import { Page } from "../../../components/layout/Page.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { Plus, CarFront } from "lucide-react";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { VehicleCard } from "../components/VehicleCard.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { VehicleEditor } from "../components/VehicleEditor.jsx";

export function Vehicles() {
  const { setEdit, state, confirm, toast, edit } = useVehicles();
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
              <VehicleCard
                key={vehicle.id}
                vehicle={vehicle}
                setEdit={setEdit}
                confirm={confirm}
                state={state}
                toast={toast}
              />
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
        <VehicleEditor
          edit={edit}
          setEdit={setEdit}
          state={state}
          toast={toast}
        />
      )}
    </Page>
  );
}
