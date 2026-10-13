import { useResource } from "../../../shared/hooks/useResource.js";
import { storesApi } from "../../stores/api/storesApi.js";
import { productsApi } from "../api/productsApi.js";
import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";
import { useUrlQuery } from "../../../shared/hooks/useUrlQuery.js";
import { queryChoice } from "../../../shared/utils/routeQuery.js";

export function useInventory() {
  const profile = useResource(storesApi.ownPath());
  const state = useResource(
    profile.data
      ? productsApi.storeProductsPath(profile.data.slug, false)
      : null,
  );
  const { params, update } = useUrlQuery();
  const filter = queryChoice(params, "stock", ["low", "out", "all"], "all");
  const items = (state.data || []).filter((item) =>
    filter === "out"
      ? item.stock === 0
      : filter === "low"
        ? item.stock > 0 && item.stock <= (item.lowStockThreshold ?? 5)
        : true,
  );
  function updateFilter(value) {
    update({ stock: value }, { defaults: { stock: "all" } });
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
