import Editor from "../Editor.jsx";

export function RecordEditor({
  definition,
  edit,
  setEdit,
  recordApi,
  vehicleId,
  reload,
  toast,
}) {
  return (
    <Editor
      title={`${definition.singular} ${edit.id ? "dəyiş" : "əlavə et"}`}
      fields={definition.fields}
      initial={edit}
      onClose={() => setEdit(null)}
      onSave={async (values) => {
        await recordApi.save(edit, vehicleId, values);
        setEdit(null);
        reload();
        toast("Qeyd yadda saxlanıldı.");
      }}
    />
  );
}
