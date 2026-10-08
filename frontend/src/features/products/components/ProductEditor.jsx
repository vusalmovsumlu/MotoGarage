import Editor from "../../../components/common/Editor.jsx";
import { productFields, stockField } from "../utils/productFields.js";
import { productsApi } from "../api/productsApi.js";

export function ProductEditor({ edit, setEdit, state, toast }) {
  return (
    <Editor
      title={edit.id ? "Məhsulu dəyiş" : "Yeni məhsul"}
      initial={edit}
      fields={[...productFields, ...(!edit.id ? [stockField] : [])]}
      image
      onClose={() => setEdit(null)}
      onSave={async (values) => {
        await productsApi.save(edit, values);
        setEdit(null);
        state.reload();
        toast("Məhsul yadda saxlanıldı.");
      }}
    />
  );
}
