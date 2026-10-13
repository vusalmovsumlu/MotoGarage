import { useResource } from "../../../shared/hooks/useResource.js";
import { adminApi } from "../api/adminApi.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { useUrlFilterForm } from "../../../shared/hooks/useUrlFilterForm.js";
import { queryText, queryChoice } from "../../../shared/utils/routeQuery.js";

const keys = ["search", "role"];
const readFilters = (params) => ({
  search: queryText(params, "search"),
  role: queryChoice(params, "role", ["User", "Mechanic", "Store", "Admin"], ""),
});

export function useAdminUsers() {
  const {
    filters,
    form,
    setForm,
    apply: setFilters,
  } = useUrlFilterForm(readFilters, keys);
  const state = useResource(adminApi.usersPath(filters));
  const { user, confirm, toast } = useApp();

  return {
    setFilters,
    search: form.search,
    role: form.role,
    setSearch: (search) => setForm((current) => ({ ...current, search })),
    setRole: (role) => setForm((current) => ({ ...current, role })),
    state,
    user,
    confirm,
    toast,
  };
}
