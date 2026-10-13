import { useResource } from "./useResource.js";
import { useUrlFilterForm } from "./useUrlFilterForm.js";
import { queryText, queryPrice } from "../utils/routeQuery.js";

const merchantKeys = ["search", "address"];
const productKeys = ["search", "brand", "minPrice", "maxPrice"];
const readMerchants = (params) => ({
  search: queryText(params, "search"),
  address: queryText(params, "address"),
});
const readProducts = (params) => {
  let minPrice = queryPrice(params, "minPrice"),
    maxPrice = queryPrice(params, "maxPrice");
  if (minPrice !== "" && maxPrice !== "" && Number(minPrice) > Number(maxPrice))
    minPrice = maxPrice = "";
  return {
    search: queryText(params, "search"),
    brand: queryText(params, "brand"),
    minPrice,
    maxPrice,
  };
};

export function useCatalog({ type, resourceApi }) {
  const products = type === "products";
  const mechanic = type === "mechanics";
  const { filters, form, setForm, apply, error, setError } = useUrlFilterForm(
    products ? readProducts : readMerchants,
    products ? productKeys : merchantKeys,
  );
  const setFilters = (values) =>
    apply(
      products
        ? {
            ...values,
            minPrice:
              values.minPrice === "" ? "" : String(Number(values.minPrice)),
            maxPrice:
              values.maxPrice === "" ? "" : String(Number(values.maxPrice)),
          }
        : values,
    );
  const search = form.search,
    extra = form[products ? "brand" : "address"],
    min = form.minPrice || "",
    max = form.maxPrice || "";
  const field = (key) => (value) =>
    setForm((current) => ({ ...current, [key]: value }));
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
    setSearch: field("search"),
    setExtra: field(products ? "brand" : "address"),
    setMin: field("minPrice"),
    setMax: field("maxPrice"),
    error,
    state,
  };
}
