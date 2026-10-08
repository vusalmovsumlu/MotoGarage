import { Thumbnail } from "../../../components/common/Thumbnail.jsx";
import { Package, Pencil, Trash2 } from "lucide-react";
import { money, number } from "../../../shared/utils/format.js";
import { IconButton } from "../../../components/ui/IconButton.jsx";
import { productsApi } from "../api/productsApi.js";

export function InventoryRow({
  item,
  setStock,
  setEdit,
  confirm,
  state,
  toast,
}) {
  return (
    <div className="inventory-row" key={item.id}>
      <Thumbnail src={item.imageUrl} icon={Package} />
      <div className="record-info">
        <a href={`#/products/${item.slug}`}>
          <strong>{item.name}</strong>
        </a>
        <span className="meta">
          {item.brand || "Marka qeyd edilməyib"} · {money(item.price)}
        </span>
      </div>
      <button className="stock-button" onClick={() => setStock(item)}>
        {number(item.stock)} ədəd <Pencil size={13} />
      </button>
      <div className="row-actions">
        <IconButton label="Məhsulu dəyiş" onClick={() => setEdit(item)}>
          <Pencil size={16} />
        </IconButton>
        <IconButton
          label="Məhsulu deaktiv et"
          onClick={() =>
            confirm({
              title: "Məhsulu deaktiv etmək istəyirsiniz?",
              description:
                "Məhsul yeni alışlar üçün gizlənəcək. Əvvəlki sifarişlər saxlanılır.",
              action: async () => {
                await productsApi.remove(item.slug);
                state.reload();
                toast("Məhsul deaktiv edildi.");
              },
            })
          }
        >
          <Trash2 size={16} />
        </IconButton>
      </div>
    </div>
  );
}
