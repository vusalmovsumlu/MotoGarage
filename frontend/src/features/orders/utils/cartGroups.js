export function groupCartByStore(cart) {
  const groups = new Map();
  for (const item of cart) {
    if (!groups.has(item.storeId)) {
      groups.set(item.storeId, {
        storeId: item.storeId,
        name: item.storeName || "Mağaza",
        items: [],
        total: 0,
      });
    }
    const group = groups.get(item.storeId);
    group.items.push(item);
    group.total += item.price * item.quantity;
  }
  return [...groups.values()];
}
