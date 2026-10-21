import { useApp } from "../../../shared/hooks/useApp.js";
import { useRef, useState } from "react";
import { ordersApi } from "../api/ordersApi.js";
import { navigate } from "../../../app/router/useRoute.js";
import { groupCartByStore } from "../utils/cartGroups.js";

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
  const submitting = useRef(false);
  const [error, setError] = useState("");
  const storeGroups = groupCartByStore(cart);
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
    if (submitting.current || !cart.length) return;
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
    submitting.current = true;
    setBusy(true);
    setError("");
    try {
      const result = await ordersApi.checkout({
        ...address,
        items: cart.map((item) => ({
          productId: item.id,
          quantity: item.quantity,
        })),
      });
      setCart([]);
      toast(
        result.orders.length > 1
          ? `${result.orders.length} sifariş yaradıldı.`
          : "Sifariş yaradıldı.",
      );
      navigate("/orders");
    } catch (failure) {
      setError(failure.message);
    } finally {
      submitting.current = false;
      setBusy(false);
    }
  };

  return {
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
  };
}
