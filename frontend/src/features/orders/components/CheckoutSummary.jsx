import { money } from "../../../shared/utils/format.js";
import { Button } from "../../../components/ui/Button.jsx";
import { ArrowRight } from "lucide-react";
import { DeliveryAddressForm } from "./DeliveryAddressForm.jsx";

export function CheckoutSummary({
  cart,
  total,
  error,
  busy,
  checkout,
  delivery,
  setDelivery,
}) {
  return (
    <aside className="card checkout-card">
      <form onSubmit={checkout}>
        <h2>Sifarişin xülasəsi</h2>
        <div className="summary-row">
          <span>Məhsul sayı</span>
          <strong>{cart.reduce((sum, item) => sum + item.quantity, 0)}</strong>
        </div>
        <div className="summary-row">
          <span>Ümumi məbləğ</span>
          <strong>{money(total)}</strong>
        </div>
        <p className="meta">
          Yekun qiymət və stok sifariş yaradılarkən mağazanın cari məlumatlarına
          görə yoxlanılır.
        </p>
        {error && (
          <p role="alert" className="form-error">
            {error}
          </p>
        )}
        <DeliveryAddressForm
          value={delivery}
          onChange={setDelivery}
          busy={busy}
        />
        <Button className="full-width" busy={busy} type="submit">
          Sifarişi təsdiqlə <ArrowRight size={16} />
        </Button>
      </form>
    </aside>
  );
}
