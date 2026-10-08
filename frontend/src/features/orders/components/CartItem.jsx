import { Thumbnail } from "../../../components/common/Thumbnail.jsx";
import { Package, Minus, Plus, Trash2 } from "lucide-react";
import { money } from "../../../shared/utils/format.js";
import { IconButton } from "../../../components/ui/IconButton.jsx";

export function CartItem({ item, busy, update, setCart }) {
  return (
    <div className="cart-row" key={item.id}>
      <Thumbnail src={item.imageUrl} icon={Package} />
      <div className="record-info">
        <strong>{item.name}</strong>
        <span className="meta">{money(item.price)} / ədəd</span>
      </div>
      <div className="quantity">
        <IconButton
          label="Sayını azalt"
          disabled={busy || item.quantity === 1}
          onClick={() => update(item.id, item.quantity - 1)}
        >
          <Minus size={14} />
        </IconButton>
        <input
          aria-label={`${item.name} sayı`}
          type="number"
          min={1}
          max={Math.min(item.stock, 10000)}
          value={item.quantity}
          disabled={busy}
          onChange={(event) => update(item.id, Number(event.target.value))}
        />
        <IconButton
          label="Sayını artır"
          disabled={
            busy || item.quantity >= item.stock || item.quantity >= 10000
          }
          onClick={() => update(item.id, item.quantity + 1)}
        >
          <Plus size={14} />
        </IconButton>
      </div>
      <strong>{money(item.price * item.quantity)}</strong>
      <IconButton
        label="Məhsulu səbətdən çıxar"
        disabled={busy}
        onClick={() =>
          setCart((current) => current.filter((value) => value.id !== item.id))
        }
      >
        <Trash2 size={16} />
      </IconButton>
    </div>
  );
}
