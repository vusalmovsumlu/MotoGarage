import Editor from "../../../components/common/Editor.jsx";
import { vehicleFields } from "../utils/vehicleFields.js";
import { garageApi } from "../api/garageApi.js";

export function VehicleEditor({ edit, setEdit, state, toast }) {
  return (
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
        await garageApi.save(edit, body);
        setEdit(null);
        state.reload();
        toast("Nəqliyyat yadda saxlanıldı.");
      }}
    />
  );
}
