import { useState } from "react";
import {
  MapPin,
  Package,
  Pencil,
  Plus,
  Store,
  Trash2,
  Wrench,
} from "lucide-react";
import { useApp } from "../context";
import { api } from "../lib/api";
import { useResource } from "../lib/hooks";
import { money, number } from "../lib/format";
import { businessFields, productFields, stockField } from "../lib/fields";
import {
  Button,
  Empty,
  IconButton,
  Page,
  Resource,
  Thumbnail,
} from "../components/ui";
import Editor from "../components/Editor";

export function BusinessProfile() {
  const { user, toast, confirm } = useApp();
  const mechanic = user.role === "Mechanic";
  const endpoint = mechanic ? "/mechanics" : "/stores";
  const state = useResource(`${endpoint}/my-profile`);
  const [edit, setEdit] = useState(null);
  return (
    <Page
      title={mechanic ? "Usta profilim" : "Mağaza profilim"}
      description="Müştərilərinizin sizi tanıdığı yer."
      actions={
        state.data && (
          <Button variant="secondary" onClick={() => setEdit(state.data)}>
            <Pencil size={16} /> Profili dəyiş
          </Button>
        )
      }
    >
      <Resource state={state} allow404 skeleton="detail">
        {state.data ? (
          <div className="card merchant-detail">
            <Thumbnail
              src={state.data.imageUrl}
              icon={mechanic ? Wrench : Store}
            />
            <div>
              <h2>{state.data.businessName || state.data.storeName}</h2>
              <p className="muted">{state.data.description}</p>
              <p className="address">
                <MapPin size={16} />
                {state.data.address}
              </p>
              <p>
                <a href={`tel:${state.data.phoneNumber}`}>
                  {state.data.phoneNumber}
                </a>
              </p>
              {state.data.hourlyRate != null && (
                <p>{money(state.data.hourlyRate)} / saat</p>
              )}
              <div className="actions">
                <a
                  className="button secondary"
                  href={`#${endpoint}/${state.data.slug}`}
                >
                  Müştəri görünüşü
                </a>
                <Button
                  variant="ghost"
                  className="danger-text"
                  onClick={() =>
                    confirm({
                      title: "Biznes profilini deaktiv etmək istəyirsiniz?",
                      description:
                        "Profiliniz kataloqdan gizlənəcək. Əvvəlki sifariş və görüş tarixçəsi qalacaq.",
                      action: async () => {
                        await api(`${endpoint}/my-profile`, {
                          method: "DELETE",
                        });
                        state.reload();
                        toast("Biznes profili deaktiv edildi.");
                      },
                    })
                  }
                >
                  Deaktiv et
                </Button>
              </div>
              {!state.data.isActive && (
                <p className="notice">
                  Profil hazırda deaktivdir və müştəri kataloqunda görünmür.
                </p>
              )}
            </div>
          </div>
        ) : (
          <Empty
            icon={mechanic ? Wrench : Store}
            title="Biznesinizi tanıdaq"
            description="Ad, ünvan və əlaqə məlumatlarını əlavə edin."
            action={
              <Button onClick={() => setEdit({})}>
                <Plus size={16} /> Profil yarat
              </Button>
            }
          />
        )}
      </Resource>
      {edit && (
        <Editor
          title={state.data ? "Biznes profilini dəyiş" : "Biznes profili yarat"}
          fields={businessFields(user.role)}
          initial={edit}
          image
          onClose={() => setEdit(null)}
          onSave={async (body) => {
            await api(state.data ? `${endpoint}/my-profile` : endpoint, {
              method: state.data ? "PUT" : "POST",
              body,
            });
            setEdit(null);
            state.reload();
            toast("Biznes profili yadda saxlanıldı.");
          }}
        />
      )}
    </Page>
  );
}
export function Inventory() {
  const profile = useResource("/stores/my-profile");
  const state = useResource(
    profile.data ? `/products/store/${profile.data.slug}` : null,
  );
  const [edit, setEdit] = useState(null);
  const [stock, setStock] = useState(null);
  const { toast, confirm } = useApp();
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
                  <div className="inventory-row" key={item.id}>
                    <Thumbnail src={item.imageUrl} icon={Package} />
                    <div className="record-info">
                      <a href={`#/products/${item.slug}`}>
                        <strong>{item.name}</strong>
                      </a>
                      <span className="meta">
                        {item.brand || "Marka qeyd edilməyib"} ·{" "}
                        {money(item.price)}
                      </span>
                    </div>
                    <button
                      className="stock-button"
                      onClick={() => setStock(item)}
                    >
                      {number(item.stock)} ədəd <Pencil size={13} />
                    </button>
                    <div className="row-actions">
                      <IconButton
                        label="Məhsulu dəyiş"
                        onClick={() => setEdit(item)}
                      >
                        <Pencil size={16} />
                      </IconButton>
                      <IconButton
                        label="Məhsulu deaktiv et"
                        onClick={() =>
                          confirm({
                            title: "Məhsulu deaktiv etmək istəyirsiniz?",
                            description:
                              "Məhsul yeni alışlar üçün gizlənəcək. Əvvəlki sifarişlər saxlanılır.",
                            action: async () => {
                              await api(`/products/${item.slug}`, {
                                method: "DELETE",
                              });
                              state.reload();
                              toast("Məhsul deaktiv edildi.");
                            },
                          })
                        }
                      >
                        <Trash2 size={16} />
                      </IconButton>
                    </div>
                  </div>
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
        <Editor
          title={edit.id ? "Məhsulu dəyiş" : "Yeni məhsul"}
          initial={edit}
          fields={[...productFields, ...(!edit.id ? [stockField] : [])]}
          image
          onClose={() => setEdit(null)}
          onSave={async (values) => {
            await api(edit.id ? `/products/${edit.slug}` : "/products", {
              method: edit.id ? "PUT" : "POST",
              body: values,
            });
            setEdit(null);
            state.reload();
            toast("Məhsul yadda saxlanıldı.");
          }}
        />
      )}
      {stock && (
        <Editor
          title="Stoku yenilə"
          description="Cari stokun yekun sayını daxil edin."
          initial={stock}
          fields={[stockField]}
          onClose={() => setStock(null)}
          onSave={async (values) => {
            await api(`/products/${stock.slug}/stock`, {
              method: "PATCH",
              body: values,
            });
            setStock(null);
            state.reload();
            toast("Stok yeniləndi.");
          }}
        />
      )}
    </Page>
  );
}
