import { Catalog } from "../../../components/common/catalog/Catalog.jsx";
import { productsApi } from "../api/productsApi.js";
import { ProductCard } from "../components/ProductCard.jsx";
export function ProductCatalog() {
  return (
    <Catalog
      type="products"
      resourceApi={productsApi}
      ProductCard={ProductCard}
    />
  );
}
