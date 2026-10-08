export const dashboardApi = {
  summaryPath: (role) => (role === "Admin" ? "/admin/dashboard" : "/dashboard"),
  queuePath: (role) =>
    role === "Mechanic"
      ? "/appointments/mechanic"
      : role === "Store"
        ? "/orders/store"
        : null,
};
