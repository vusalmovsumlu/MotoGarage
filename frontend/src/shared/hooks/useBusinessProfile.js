import { useApp } from "./useApp.js";
import { useResource } from "./useResource.js";
import { useState } from "react";

export function useBusinessProfile({ resourceApi }) {
  const { user, toast, confirm } = useApp();
  const mechanic = user.role === "Mechanic";
  const endpoint = mechanic ? "/mechanics" : "/stores";
  const state = useResource(resourceApi.ownPath());
  const [edit, setEdit] = useState(null);

  return { mechanic, state, setEdit, endpoint, confirm, toast, edit, user };
}
