import { useEffect, useMemo, useState } from "react";
import { useUrlQuery } from "./useUrlQuery.js";
import { routeParams, subscribeHashRoute } from "../utils/routeQuery.js";

// Draft inputs stay local until submission. Navigation restores applied URL
// filters, including when history returns to a previously edited search.
export function useUrlFilterForm(decode, keys) {
  const query = useUrlQuery();
  const filters = useMemo(() => decode(query.params), [decode, query.params]);
  const [draft, setDraft] = useState(() => ({ values: filters, error: "" }));
  useEffect(
    () =>
      subscribeHashRoute(() => {
        setDraft({ values: decode(routeParams()), error: "" });
      }),
    [decode],
  );
  const setForm = (update) =>
    setDraft((current) => ({
      ...current,
      values: typeof update === "function" ? update(current.values) : update,
    }));
  const setError = (error) => setDraft((current) => ({ ...current, error }));
  const apply = (values, options) => {
    query.update(
      { ...Object.fromEntries(keys.map((key) => [key, null])), ...values },
      options,
    );
    setDraft({ values: decode(routeParams()), error: "" });
  };
  return {
    ...query,
    filters,
    form: draft.values,
    setForm,
    apply,
    error: draft.error,
    setError,
  };
}
