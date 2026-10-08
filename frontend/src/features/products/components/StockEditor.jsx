import Editor from "../../../components/common/Editor.jsx";
import { stockField } from "../utils/productFields.js";
import { productsApi } from "../api/productsApi.js";

export function StockEditor({ stock, setStock, state, toast }) {
  return (
    <Editor
      title="Stoku yenilə"
      description="Cari stokun yekun sayını daxil edin."
      initial={stock}
      fields={[stockField]}
      onClose={() => setStock(null)}
      onSave={async (values) => {
        await productsApi.updateStock(stock.slug, values);
        setStock(null);
        state.reload();
        toast("Stok yeniləndi.");
      }}
    />
  );
}
