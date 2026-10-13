import { useApp } from "../../../shared/hooks/useApp.js";
import { useState } from "react";
import { ordersApi } from "../api/ordersApi.js";
import { navigate } from "../../../app/router/useRoute.js";

export function useCheckout() {
  const { cart, setCart, toast, user } = useApp();
  const [delivery, setDelivery] = useState({
    recipientName: user.fullName || "",
    deliveryPhone: "",
    deliveryCity: "",
    deliveryAddress: "",
    deliveryNote: "",
  });
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
  const checkout = async (event) => {
    event?.preventDefault();
    if (busy || !cart.length) return;
    const address = Object.fromEntries(
      Object.entries(delivery).map(([key, value]) => [key, value.trim()]),
    );
    if (
      [
        "recipientName",
        "deliveryPhone",
        "deliveryCity",
        "deliveryAddress",
      ].some((key) => !address[key])
    ) {
      setError("Alıcının adı, telefon, şəhər və tam ünvan tələb olunur.");
      return;
    }
    if (
      !/^\+?[0-9 ()-]{7,30}$/.test(address.deliveryPhone) ||
      address.deliveryPhone.replace(/\D/g, "").length < 7 ||
      address.deliveryPhone.replace(/\D/g, "").length > 15
    ) {
      setError("Düzgün telefon nömrəsi yazın.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      await ordersApi.create({
        ...address,
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

  return {
    cart,
    busy,
    update,
    setCart,
    total,
    error,
    checkout,
    delivery,
    setDelivery,
  };
}
