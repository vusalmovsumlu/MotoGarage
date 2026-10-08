import { api } from "../../../shared/api/client.js";
import { query } from "../../../shared/utils/query.js";

export const productsApi = {
  listPath: (filters) => `/products${query(filters)}`,
  detailPath: (slug) => `/products/${encodeURIComponent(slug)}`,
  storeProductsPath: (slug, encode = true) =>
    `/products/store/${encode ? encodeURIComponent(slug) : slug}`,
  save: (edit, body) =>
    api(edit.id ? `/products/${edit.slug}` : "/products", {
      method: edit.id ? "PUT" : "POST",
      body,
    }),
  remove: (slug) => api(`/products/${slug}`, { method: "DELETE" }),
  updateStock: (slug, body) =>
    api(`/products/${slug}/stock`, { method: "PATCH", body }),
};
