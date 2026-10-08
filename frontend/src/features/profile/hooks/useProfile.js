import { useResource } from "../../../shared/hooks/useResource.js";
import { profileApi } from "../api/profileApi.js";
import { useState } from "react";
import { useApp } from "../../../shared/hooks/useApp.js";

export function useProfile() {
  const state = useResource(profileApi.path());
  const [edit, setEdit] = useState(false);
  const { toast, updateUser } = useApp();
  const profile = state.data;

  return { profile, setEdit, state, edit, updateUser, toast };
}
