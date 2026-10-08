import { useState } from "react";
import { useResource } from "./useResource.js";

export function useCatalog({ type, resourceApi }) {
  const products = type === "products";
  const mechanic = type === "mechanics";
  const [search, setSearch] = useState("");
  const [extra, setExtra] = useState("");
  const [min, setMin] = useState("");
  const [max, setMax] = useState("");
  const [filters, setFilters] = useState({});
  const [error, setError] = useState("");
  const state = useResource(resourceApi.listPath(filters));

  return {
    products,
    mechanic,
    min,
    max,
    setError,
    setFilters,
    search,
    extra,
    setSearch,
    setExtra,
    setMin,
    setMax,
    error,
    state,
  };
}
