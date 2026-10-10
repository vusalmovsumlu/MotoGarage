export const dashboardApi = {
  summaryPath: (role) =>
    role === "Admin"
      ? "/admin/dashboard"
      : role === "User"
        ? "/dashboard"
        : null,
  queuePath: (role) =>
    role === "Mechanic"
      ? "/appointments/mechanic"
      : role === "Store"
        ? "/orders/store"
        : null,
};
