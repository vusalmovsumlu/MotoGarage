import Editor from "../Editor.jsx";
import { businessFields } from "./businessFields.js";

export function BusinessProfileEditor({
  state,
  user,
  edit,
  setEdit,
  resourceApi,
  toast,
}) {
  return (
    <Editor
      title={state.data ? "Biznes profilini dəyiş" : "Biznes profili yarat"}
      fields={businessFields(user.role)}
      initial={edit}
      image
      onClose={() => setEdit(null)}
      onSave={async (body) => {
        await resourceApi.saveProfile(state.data, body);
        setEdit(null);
        state.reload();
        toast("Biznes profili yadda saxlanıldı.");
      }}
    />
  );
}
