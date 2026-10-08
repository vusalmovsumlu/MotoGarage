import { useState } from "react";

export function useCart(user, toast) {
  const [cartState, setCartState] = useState({ ownerId: null, items: [] });
  const cart = cartState.ownerId === user?.id ? cartState.items : [];
  const setCart = (update) =>
    setCartState((current) => ({
      ownerId: user?.id,
      items:
        typeof update === "function"
          ? update(current.ownerId === user?.id ? current.items : [])
          : update,
    }));
  const addCart = (product) => {
    if (cart.length && cart[0].storeId !== product.storeId) {
      toast(
        "Bir sifarişdə yalnız bir mağazanın məhsulları ola bilər. Əvvəl hazırkı səbəti tamamlayın.",
        true,
      );
      return;
    }
    const item = cart.find((value) => value.id === product.id);
    if (item && item.quantity >= Math.min(product.stock, 10000)) {
      toast("Stokda olan saydan çox əlavə edə bilməzsiniz.", true);
      return;
    }
    if (!item && cart.length >= 100) {
      toast(
        "Bir sifariş maksimum 100 fərqli məhsuldan ibarət ola bilər.",
        true,
      );
      return;
    }
    setCart((current) =>
      item
        ? current.map((value) =>
            value.id === product.id
              ? { ...value, quantity: value.quantity + 1 }
              : value,
          )
        : [...current, { ...product, quantity: 1 }],
    );
    toast("Məhsul səbətə əlavə edildi.");
  };
  return { cart, setCart, addCart };
}
