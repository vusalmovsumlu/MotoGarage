export const vehiclePath = (vehicle, kind) =>
  `/garage/${encodeURIComponent(vehicle.slug)}${kind ? `/${kind}` : ""}`;

export function vehicleSlugFromRoute(route) {
  const parts = route.split("?")[0].split("/").filter(Boolean);
  if (parts[0] !== "garage" || !parts[1]) return null;
  try {
    return decodeURIComponent(parts[1]);
  } catch {
    return null;
  }
}
