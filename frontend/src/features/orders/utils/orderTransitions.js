export const orderTransitions = {
  Pending: ["Confirmed", "Cancelled"],
  Confirmed: ["Preparing", "Cancelled"],
  Preparing: ["Shipped", "Cancelled"],
  Shipped: ["Completed"],
};
