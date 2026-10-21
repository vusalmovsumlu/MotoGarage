import { Store } from "lucide-react";
import { money } from "../../../shared/utils/format.js";
import { CartItem } from "./CartItem.jsx";

export function CartStoreGroup({ group, busy, update, setCart }) {
  return (
    <section
      className="card rows-card cart-store-group"
      aria-label={group.name}
    >
      <div className="cart-store-header">
        <h2>
          <Store size={18} /> {group.name}
        </h2>
        <strong>{money(group.total)}</strong>
      </div>
      {group.items.map((item) => (
        <CartItem
          key={item.id}
          item={item}
          busy={busy}
          update={update}
          setCart={setCart}
        />
      ))}
    </section>
  );
}
