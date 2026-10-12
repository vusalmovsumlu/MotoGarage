import { useResource } from "../../../shared/hooks/useResource.js";
import { storesApi } from "../../stores/api/storesApi.js";
import { productsApi } from "../api/productsApi.js";
import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";

export function useInventory(route) {
  const profile = useResource(storesApi.ownPath());
  const state = useResource(
    profile.data
      ? productsApi.storeProductsPath(profile.data.slug, false)
      : null,
  );
  const requestedFilter = new URLSearchParams(route?.split("?")[1]).get(
    "stock",
  );
  const filter = ["low", "out"].includes(requestedFilter)
    ? requestedFilter
    : "all";
  const items = (state.data || []).filter((item) =>
    filter === "out"
      ? item.stock === 0
      : filter === "low"
        ? item.stock > 0 && item.stock <= (item.lowStockThreshold ?? 5)
        : true,
  );
  function updateFilter(value) {
    window.location.hash =
      value === "all" ? "/inventory" : `/inventory?stock=${value}`;
  }
  const [edit, setEdit] = useState(null);
  const [stock, setStock] = useState(null);
  const { toast, confirm } = useApp();

  return {
    profile,
    setEdit,
    state,
    setStock,
    confirm,
    toast,
    edit,
    stock,
    filter,
    updateFilter,
    items,
  };
}
