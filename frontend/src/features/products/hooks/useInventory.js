import { useResource } from "../../../shared/hooks/useResource.js";
import { storesApi } from "../../stores/api/storesApi.js";
import { productsApi } from "../api/productsApi.js";
import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";

export function useInventory() {
  const profile = useResource(storesApi.ownPath());
  const state = useResource(
    profile.data
      ? productsApi.storeProductsPath(profile.data.slug, false)
      : null,
  );
  const [edit, setEdit] = useState(null);
  const [stock, setStock] = useState(null);
  const { toast, confirm } = useApp();

  return { profile, setEdit, state, setStock, confirm, toast, edit, stock };
}
