import Editor from "../../../components/common/Editor.jsx";
import { productFields, stockField } from "../utils/productFields.js";
import { productsApi } from "../api/productsApi.js";

export function ProductEditor({ edit, setEdit, state, toast }) {
  return (
    <Editor
      title={edit.id ? "Məhsulu dəyiş" : "Yeni məhsul"}
      initial={edit}
      fields={[
        ...productFields,
        {
          key: "lowStockThreshold",
          label: "Stok xəbərdarlıq həddi",
          type: "number",
          required: true,
          min: 0,
          max: 10000000,
          default: 5,
          hint: "Stok bu həddə düşəndə bir bildiriş göndərilir.",
        },
        ...(!edit.id ? [stockField] : []),
      ]}
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
