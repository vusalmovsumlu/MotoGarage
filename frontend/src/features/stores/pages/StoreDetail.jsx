import { MerchantDetail } from "../../../components/common/merchants/MerchantDetail.jsx";
import { storesApi } from "../api/storesApi.js";
import { chatApi } from "../../chat/api/chatApi.js";
import { productsApi } from "../../products/api/productsApi.js";
import { ProductCard } from "../../products/components/ProductCard.jsx";
const extrasPath = (item, slug) => productsApi.storeProductsPath(slug);
export function StoreDetail({ slug }) {
  return (
    <MerchantDetail
      type="stores"
      slug={slug}
      resourceApi={storesApi}
      extrasPath={extrasPath}
      chatApi={chatApi}
      ProductCard={ProductCard}
    />
  );
}
