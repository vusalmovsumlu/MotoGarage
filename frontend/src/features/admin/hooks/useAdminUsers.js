import { useState } from "react";
import { useResource } from "../../../shared/hooks/useResource.js";
import { adminApi } from "../api/adminApi.js";
import { useApp } from "../../../shared/hooks/useApp.js";

export function useAdminUsers() {
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");
  const [filters, setFilters] = useState({});
  const state = useResource(adminApi.usersPath(filters));
  const { user, confirm, toast } = useApp();

  return {
    setFilters,
    search,
    role,
    setSearch,
    setRole,
    state,
    user,
    confirm,
    toast,
  };
}
