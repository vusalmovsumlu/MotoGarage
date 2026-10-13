import { useResource } from "./useResource.js";
import { useState, useEffect } from "react";
import { useApp } from "./useApp.js";
import { Fuel, Wrench, Wallet, Bell } from "lucide-react";
import { vehiclePath, vehicleSlugFromRoute } from "../utils/vehicleRoutes.js";
import { replaceRoute } from "../../app/router/useRoute.js";
import { useUrlFilterForm } from "./useUrlFilterForm.js";
import { queryDateRange } from "../utils/routeQuery.js";

const dateKeys = ["startDate", "endDate"];
const readRange = (params) =>
  queryDateRange(params, "startDate", "endDate", {
    startDate: "",
    endDate: "",
  });

export function useVehicleRecords({ kind, route, definition, vehicleApi }) {
  const recordApi = definition.recordApi;
  const vehicles = useResource(vehicleApi.listPath());
  const query = useUrlFilterForm(readRange, dateKeys);
  const slug = vehicleSlugFromRoute(route);
  const legacyVehicleId = new URLSearchParams(route.split("?")[1]).get(
    "vehicle",
  );
  const legacyVehicle = vehicles.data?.find(
    (item) => String(item.id) === legacyVehicleId,
  );
  useEffect(() => {
    if (!slug && legacyVehicle?.slug) {
      const query = new URLSearchParams(route.split("?")[1]);
      query.delete("vehicle");
      const remaining = query.toString();
      replaceRoute(
        `${vehiclePath(legacyVehicle, kind)}${remaining ? `?${remaining}` : ""}`,
      );
    }
  }, [slug, legacyVehicle, kind, route]);
  const routedVehicle = slug
    ? vehicles.data?.find((item) => item.slug === slug)
    : null;
  const vehicleId = slug
    ? routedVehicle
      ? String(routedVehicle.id)
      : ""
    : legacyVehicleId ||
      (vehicles.data?.length ? String(vehicles.data[0].id) : "");
  const setVehicleId = (id) => {
    const vehicle = vehicles.data?.find((item) => item.id === Number(id));
    if (vehicle)
      query.update(
        {},
        { path: vehiclePath(vehicle, kind), remove: ["vehicle"] },
      );
  };
  const range = ["fuel", "expenses"].includes(kind) ? query.filters : {};
  const startDate = query.form.startDate,
    endDate = query.form.endDate;
  const setStart = (value) =>
    query.setForm((current) => ({ ...current, startDate: value }));
  const setEnd = (value) =>
    query.setForm((current) => ({ ...current, endDate: value }));
  const filterError = query.error,
    setFilterError = query.setError;
  const setRange = (values) => {
    const vehicle = vehicles.data?.find(
      (item) => item.id === Number(vehicleId),
    );
    query.apply(values, {
      ...(vehicle?.slug ? { path: vehiclePath(vehicle, kind) } : {}),
      remove: ["vehicle"],
    });
  };
  const [edit, setEdit] = useState(null);
  const { toast, confirm, act } = useApp();
  const state = useResource(
    vehicleId ? recordApi.listPath(vehicleId, range) : null,
  );
  const stats = useResource(
    kind === "fuel" && vehicleId ? recordApi.statsPath(vehicleId, range) : null,
  );
  const records = (
    Array.isArray(state.data) ? state.data : state.data?.expenses || []
  ).filter(
    (item) =>
      kind !== "fuel" ||
      !range.startDate ||
      (item.fuelDate.slice(0, 10) >= range.startDate &&
        item.fuelDate.slice(0, 10) <= range.endDate),
  );
  const reload = () => {
    state.reload();
    stats.reload();
    vehicles.reload();
  };
  const activeVehicle = vehicles.data?.find(
    (item) => item.id === Number(vehicleId),
  );
  const Icon = {
    fuel: Fuel,
    service: Wrench,
    expenses: Wallet,
    reminders: Bell,
  }[kind];

  return {
    activeVehicle,
    setEdit,
    vehicles:
      slug && vehicles.data && !routedVehicle
        ? { ...vehicles, error: new Error("Nəqliyyat vasitəsi tapılmadı.") }
        : vehicles,
    vehicleId,
    setVehicleId,
    startDate,
    endDate,
    setFilterError,
    setRange,
    setStart,
    setEnd,
    filterError,
    stats,
    state,
    records,
    Icon,
    act,
    recordApi,
    reload,
    confirm,
    toast,
    edit,
  };
}
