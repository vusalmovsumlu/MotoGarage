import { useApp } from "../../../shared/hooks/useApp.js";
import { useState } from "react";
import { ordersApi } from "../api/ordersApi.js";
import { navigate } from "../../../app/router/useRoute.js";

export function useCheckout() {
  const { cart, setCart, toast } = useApp();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const total = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
  const update = (id, quantity) =>
    setCart((current) =>
      current.map((item) =>
        item.id === id
          ? {
              ...item,
              quantity: Math.max(1, Math.min(item.stock, 10000, quantity)),
            }
          : item,
      ),
    );
  const checkout = async () => {
    setBusy(true);
    setError("");
    try {
      await ordersApi.create({
        items: cart.map((item) => ({
          productId: item.id,
          quantity: item.quantity,
        })),
      });
      setCart([]);
      toast("Sifariş yaradıldı.");
      navigate("/orders");
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  };

  return { cart, busy, update, setCart, total, error, checkout };
}
