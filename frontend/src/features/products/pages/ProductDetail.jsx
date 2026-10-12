import { useResource } from "../../../shared/hooks/useResource.js";
import { productsApi } from "../api/productsApi.js";
import { useApp } from "../../../shared/hooks/useApp.js";
import { storesApi } from "../../stores/api/storesApi.js";
import { Page } from "../../../components/layout/Page.jsx";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { Thumbnail } from "../../../components/common/Thumbnail.jsx";
import { Package, ShoppingBag, MessageCircle, ArrowRight } from "lucide-react";
import { money, number } from "../../../shared/utils/format.js";
import { Button } from "../../../components/ui/Button.jsx";
import { chatApi } from "../../chat/api/chatApi.js";
import { navigate } from "../../../app/router/useRoute.js";

export function ProductDetail({ slug }) {
  const state = useResource(productsApi.detailPath(slug));
  const { addCart, act } = useApp();
  const stores = useResource(storesApi.listPath());
  const item = state.data;
  const store = stores.data?.find((value) => value.id === item?.storeId);
  return (
    <Page title={item?.name || "Məhsul"} description={item?.brand}>
      <Resource state={state} skeleton="detail">
        {item && (
          <div className="card product-detail">
            <Thumbnail src={item.imageUrl} icon={Package} alt={item.name} />
            <div>
              <p className="eyebrow">{item.brand || "EHTİYAT HİSSƏSİ"}</p>
              <h2>{item.name}</h2>
              <p className="muted">
                {item.description ||
                  "Məhsul haqqında daha ətraflı məlumat üçün mağazaya yazın."}
              </p>
              {item.vehicleCompatibility && (
                <p className="compatibility">
                  Uyğunluq: {item.vehicleCompatibility}
                </p>
              )}
              <strong className="product-price">{money(item.price)}</strong>
              <p className="meta">
                {item.stock
                  ? `${number(item.stock)} ədəd stokda`
                  : "Stokda yoxdur"}
              </p>
              <div className="actions">
                <Button disabled={!item.stock} onClick={() => addCart(item)}>
                  <ShoppingBag size={16} /> Səbətə əlavə et
                </Button>
                <Button
                  variant="secondary"
                  onClick={() =>
                    act(async () => {
                      const result = await chatApi.start("store", item.storeId);
                      navigate(chatApi.path(result));
                    })
                  }
                >
                  <MessageCircle size={16} /> Mağazaya yaz
                </Button>
              </div>
              {store && (
                <a className="text-link" href={`#/stores/${store.slug}`}>
                  {store.storeName} mağazasına keç <ArrowRight size={14} />
                </a>
              )}
            </div>
          </div>
        )}
      </Resource>
    </Page>
  );
}
