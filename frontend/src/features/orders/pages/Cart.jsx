import { useCheckout } from "../hooks/useCheckout.js";
import { Page } from "../../../components/layout/Page.jsx";
import { CartStoreGroup } from "../components/CartStoreGroup.jsx";
import { CheckoutSummary } from "../components/CheckoutSummary.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { ShoppingBag } from "lucide-react";

export function Cart() {
  const {
    cart,
    storeGroups,
    busy,
    update,
    setCart,
    total,
    error,
    checkout,
    delivery,
    setDelivery,
  } = useCheckout();
  return (
    <Page
      title="Səbətiniz"
      description="Fərqli mağazalardan məhsullarınızı bir səbətdə sifariş edin."
    >
      {cart.length ? (
        <div className="cart-layout">
          <div className="cart-groups">
            {storeGroups.map((group) => (
              <CartStoreGroup
                key={group.storeId}
                group={group}
                busy={busy}
                update={update}
                setCart={setCart}
              />
            ))}
          </div>
          <CheckoutSummary
            cart={cart}
            storeCount={storeGroups.length}
            total={total}
            error={error}
            busy={busy}
            checkout={checkout}
            delivery={delivery}
            setDelivery={setDelivery}
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
