import { useResource } from "../../../shared/hooks/useResource.js";
import { garageApi } from "../api/garageApi.js";
import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";

export function useVehicles() {
  const state = useResource(garageApi.listPath());
  const [edit, setEdit] = useState(null);
  const { toast, confirm } = useApp();

  return { setEdit, state, confirm, toast, edit };
}
