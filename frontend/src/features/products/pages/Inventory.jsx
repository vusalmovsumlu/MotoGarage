import { useInventory } from "../hooks/useInventory.js";
import { Page } from "../../../components/layout/Page.jsx";
import { Button } from "../../../components/ui/Button.jsx";
import { Plus, Package, Store } from "lucide-react";
import { Resource } from "../../../components/feedback/Resource.jsx";
import { InventoryRow } from "../components/InventoryRow.jsx";
import { Empty } from "../../../components/feedback/Empty.jsx";
import { ProductEditor } from "../components/ProductEditor.jsx";
import { StockEditor } from "../components/StockEditor.jsx";

export function Inventory() {
  const { profile, setEdit, state, setStock, confirm, toast, edit, stock } =
    useInventory();
  return (
    <Page
      title="Məhsullarım"
      description="Kataloqunuz və stokunuz nəzarətinizdə olsun."
      actions={
        profile.data?.isActive && (
          <Button onClick={() => setEdit({})}>
            <Plus size={16} /> Məhsul əlavə et
          </Button>
        )
      }
    >
      <Resource state={profile} allow404>
        {profile.data ? (
          <Resource state={state}>
            {state.data?.length ? (
              <div className="card rows-card">
                {state.data.map((item) => (
                  <InventoryRow
                    key={item.id}
                    item={item}
                    setStock={setStock}
                    setEdit={setEdit}
                    confirm={confirm}
                    state={state}
                    toast={toast}
                  />
                ))}
              </div>
            ) : (
              <Empty
                icon={Package}
                title="İlk məhsulunuzu əlavə edin"
                description="Məhsullarınız müştəri kataloqunda görünəcək."
              />
            )}
          </Resource>
        ) : (
          <Empty
            icon={Store}
            title="Əvvəl mağaza profili yaradın"
            action={
              <a className="button primary" href="#/business">
                Mağaza profilim
              </a>
            }
          />
        )}
      </Resource>
      {edit && (
        <ProductEditor
          edit={edit}
          setEdit={setEdit}
          state={state}
          toast={toast}
        />
      )}
      {stock && (
        <StockEditor
          stock={stock}
          setStock={setStock}
          state={state}
          toast={toast}
        />
      )}
    </Page>
  );
}
