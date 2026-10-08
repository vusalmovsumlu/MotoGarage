import { useCatalog } from "../../../shared/hooks/useCatalog.js";
import { Page } from "../../layout/Page.jsx";
import { CatalogFilters } from "./CatalogFilters.jsx";
import { Resource } from "../../feedback/Resource.jsx";
import { MerchantCard } from "../merchants/MerchantCard.jsx";
import { Empty } from "../../feedback/Empty.jsx";
import { Package, Wrench, Store } from "lucide-react";

export function Catalog({ type, resourceApi, ProductCard }) {
  const {
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
  } = useCatalog({ type, resourceApi });
  return (
    <Page
      title={
        products
          ? "Yol üçün lazım olanlar."
          : mechanic
            ? "Nəqliyyatınızı tanıyan ustalar."
            : "Etibar etdiyiniz mağazalar."
      }
      eyebrow={
        products ? "MƏHSUL KATALOQU" : mechanic ? "USTA KATALOQU" : "MAĞAZALAR"
      }
      description={
        products
          ? "Ehtiyat hissələri və aksesuarları rahat tapın."
          : "Axtarın, tanış olun və birbaşa əlaqə saxlayın."
      }
    >
      <CatalogFilters
        products={products}
        min={min}
        max={max}
        setError={setError}
        setFilters={setFilters}
        search={search}
        extra={extra}
        setSearch={setSearch}
        setExtra={setExtra}
        setMin={setMin}
        setMax={setMax}
      />
      {error && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
      <Resource state={state} skeleton="cards">
        <p className="results-count meta">{state.data?.length || 0} nəticə</p>
        {state.data?.length ? (
          <div className="catalog-grid">
            {state.data.map((item) =>
              products ? (
                <ProductCard key={item.id} product={item} />
              ) : (
                <MerchantCard
                  key={item.id}
                  item={item}
                  type={type}
                  mechanic={mechanic}
                />
              ),
            )}
          </div>
        ) : (
          <Empty
            icon={products ? Package : mechanic ? Wrench : Store}
            title="Nəticə tapılmadı"
            description="Axtarış sözünü və ya filterləri dəyişib yenidən yoxlayın."
          />
        )}
      </Resource>
    </Page>
  );
}
