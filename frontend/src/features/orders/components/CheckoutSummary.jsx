import { money } from "../../../shared/utils/format.js";
import { Button } from "../../../components/ui/Button.jsx";
import { ArrowRight } from "lucide-react";

export function CheckoutSummary({ cart, total, error, busy, checkout }) {
  return (
    <aside className="card checkout-card">
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
      <Button className="full-width" busy={busy} onClick={checkout}>
        Sifarişi təsdiqlə <ArrowRight size={16} />
      </Button>
    </aside>
  );
}
