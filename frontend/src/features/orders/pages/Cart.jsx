import { useCheckout } from "../hooks/useCheckout.js";
import { Page } from "../../../components/layout/Page.jsx";
import { CartItem } from "../components/CartItem.jsx";
import { CheckoutSummary } from "../components/CheckoutSummary.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { ShoppingBag } from "lucide-react";

export function Cart() {
  const { cart, busy, update, setCart, total, error, checkout } = useCheckout();
  return (
    <Page
      title="Səbətiniz"
      description="Bir sifariş bir mağazanın məhsullarından ibarətdir."
    >
      {cart.length ? (
        <div className="cart-layout">
          <div className="card rows-card">
            {cart.map((item) => (
              <CartItem
                key={item.id}
                item={item}
                busy={busy}
                update={update}
                setCart={setCart}
              />
            ))}
          </div>
          <CheckoutSummary
            cart={cart}
            total={total}
            error={error}
            busy={busy}
            checkout={checkout}
          />
        </div>
      ) : (
        <Empty
          icon={ShoppingBag}
          title="Səbətiniz hələ boşdur"
          description="Növbəti yol üçün lazım olanları kataloqdan seçin."
          action={
            <a href="#/products" className="button primary">
              Məhsullara bax
            </a>
          }
        />
      )}
    </Page>
  );
}
