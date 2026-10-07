import { useState } from "react";
import {
  ArrowRight,
  MapPin,
  MessageCircle,
  Minus,
  Package,
  Pencil,
  Phone,
  Plus,
  ShoppingBag,
  Star,
  Store,
  Trash2,
  Wrench,
} from "lucide-react";
import { api, query } from "../lib/api";
import { navigate, useResource } from "../lib/hooks";
import PeekRating from "../components/PeekRating";
import { date, money, number } from "../lib/format";
import {
  Button,
  Empty,
  IconButton,
  Page,
  Resource,
  SearchBar,
  Thumbnail,
} from "../components/ui";
import Editor from "../components/Editor";
import { AppointmentEditor } from "./Work";
import { useApp } from "../context";

export function ProductCard({ product }) {
  const { addCart } = useApp();
  return (
    <article className="card product-card">
      <a href={`#/products/${product.slug}`} className="product-image">
        <Thumbnail src={product.imageUrl} icon={Package} alt={product.name} />
      </a>
      <div className="product-body">
        <span className="meta">{product.brand || "Ehtiyat hissəsi"}</span>
        <h2>
          <a href={`#/products/${product.slug}`}>{product.name}</a>
        </h2>
        <p className="meta">
          {product.vehicleCompatibility ||
            "Uyğunluğu satıcı ilə dəqiqləşdirin."}
        </p>
        <div className="product-bottom">
          <strong>{money(product.price)}</strong>
          <span className={`stock-label ${!product.stock ? "sold-out" : ""}`}>
            {product.stock > 0
              ? `${number(product.stock)} ədəd stokda`
              : "Stokda yoxdur"}
          </span>
        </div>
        <Button
          variant="secondary"
          disabled={product.stock < 1}
          onClick={() => addCart(product)}
        >
          <Plus size={15} /> Səbətə əlavə et
        </Button>
      </div>
    </article>
  );
}
export function Catalog({ type }) {
  const products = type === "products";
  const mechanic = type === "mechanics";
  const [search, setSearch] = useState("");
  const [extra, setExtra] = useState("");
  const [min, setMin] = useState("");
  const [max, setMax] = useState("");
  const [filters, setFilters] = useState({});
  const [error, setError] = useState("");
  const state = useResource(`/${type}${query(filters)}`);
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
      <form
        onSubmit={(event) => {
          event.preventDefault();
          if (
            products &&
            (Number(min) < 0 ||
              Number(max) < 0 ||
              (min !== "" && max !== "" && Number(min) > Number(max)))
          ) {
            setError("Qiymət aralığını düzgün daxil edin.");
            return;
          }
          setError("");
          setFilters({
            search,
            [products ? "brand" : "address"]: extra,
            ...(products ? { minPrice: min, maxPrice: max } : {}),
          });
        }}
      >
        <SearchBar
          value={search}
          onChange={setSearch}
          placeholder={
            products ? "Məhsul axtarın…" : "Ad və ya xidmət axtarın…"
          }
        >
          <input
            aria-label={products ? "Marka" : "Ünvan"}
            placeholder={products ? "Marka" : "Ünvan / şəhər"}
            value={extra}
            onChange={(event) => setExtra(event.target.value)}
          />
          {products && (
            <>
              <input
                className="price-filter"
                aria-label="Minimum qiymət"
                placeholder="Min ₼"
                type="number"
                min="0"
                step="0.01"
                value={min}
                onChange={(event) => setMin(event.target.value)}
              />
              <input
                className="price-filter"
                aria-label="Maksimum qiymət"
                placeholder="Max ₼"
                type="number"
                min="0"
                step="0.01"
                value={max}
                onChange={(event) => setMax(event.target.value)}
              />
            </>
          )}
          <Button type="submit">Axtar</Button>
        </SearchBar>
      </form>
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
                <article className="card merchant-card" key={item.id}>
                  <a href={`#/${type}/${item.slug}`}>
                    <Thumbnail
                      src={item.imageUrl}
                      icon={mechanic ? Wrench : Store}
                      alt={item.businessName || item.storeName}
                    />
                  </a>
                  <div className="merchant-body">
                    <div className="merchant-rating">
                      <Star size={13} fill="currentColor" />{" "}
                      {number(item.rating)} <span className="meta">/ 5</span>
                    </div>
                    <h2>
                      <a href={`#/${type}/${item.slug}`}>
                        {item.businessName || item.storeName}
                      </a>
                    </h2>
                    <p className="muted line-clamp">{item.description}</p>
                    <p className="meta address">
                      <MapPin size={14} />
                      {item.address}
                    </p>
                    <div className="card-footer">
                      {mechanic && item.hourlyRate != null ? (
                        <strong>
                          {money(item.hourlyRate)}{" "}
                          <small className="meta">/ saat</small>
                        </strong>
                      ) : (
                        <span className="meta">
                          {mechanic ? "Qiymət razılaşma ilə" : "Mağaza profili"}
                        </span>
                      )}
                      <a className="text-link" href={`#/${type}/${item.slug}`}>
                        Ətraflı <ArrowRight size={14} />
                      </a>
                    </div>
                  </div>
                </article>
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
export function MerchantDetail({ type, slug }) {
  const mechanic = type === "mechanics";
  const state = useResource(`/${type}/${encodeURIComponent(slug)}`);
  const item = state.data;
  const extras = useResource(
    item
      ? mechanic
        ? `/reviews/mechanic/${item.id}`
        : `/products/store/${encodeURIComponent(slug)}`
      : null,
  );
  const [appointment, setAppointment] = useState(false);
  const [review, setReview] = useState(null);
  const { user, act, toast, confirm } = useApp();
  return (
    <Page
      title={item?.businessName || item?.storeName || "Biznes profili"}
      description={item?.address}
    >
      <Resource state={state} skeleton="detail">
        {item && (
          <>
            <div className="card merchant-detail">
              <Thumbnail
                src={item.imageUrl}
                icon={mechanic ? Wrench : Store}
                alt={item.businessName || item.storeName}
              />
              <div>
                <p className="eyebrow">
                  {mechanic ? "USTA PROFİLİ" : "MAĞAZA PROFİLİ"}
                </p>
                <h2>{item.businessName || item.storeName}</h2>
                <p className="muted">{item.description}</p>
                <div className="merchant-contact">
                  <span>
                    <Star size={15} />
                    {number(item.rating)} / 5
                  </span>
                  <a href={`tel:${item.phoneNumber}`}>
                    <Phone size={15} />
                    {item.phoneNumber}
                  </a>
                  {item.hourlyRate != null && (
                    <strong>{money(item.hourlyRate)} / saat</strong>
                  )}
                </div>
                {item.userId !== user.id && (
                  <div className="actions">
                    {mechanic && (
                      <Button onClick={() => setAppointment(true)}>
                        Görüş planlaşdır
                      </Button>
                    )}
                    <Button
                      variant="secondary"
                      onClick={() =>
                        act(async () => {
                          const result = await api(
                            `/chat/${mechanic ? "mechanic" : "store"}/${item.id}`,
                            { method: "POST" },
                          );
                          navigate(`/chat/${result.id}`);
                        })
                      }
                    >
                      <MessageCircle size={16} /> Mesaj yaz
                    </Button>
                  </div>
                )}
              </div>
            </div>
            <div className="section-heading">
              <h2>{mechanic ? "Müştəri rəyləri" : "Mağazanın məhsulları"}</h2>
              {mechanic &&
                item.userId !== user.id &&
                !extras.loading &&
                !extras.error &&
                !extras.data?.some((value) => value.userId === user.id) && (
                  <Button variant="secondary" onClick={() => setReview({})}>
                    <Plus size={15} /> Rəy yaz
                  </Button>
                )}
            </div>
            <Resource state={extras} skeleton={mechanic ? "list" : "cards"}>
              {extras.data?.length ? (
                mechanic ? (
                  <div className="review-grid">
                    {extras.data.map((value) => (
                      <article className="card review-card" key={value.id}>
                        <div className="review-top">
                          <strong>
                            {value.userId === user.id
                              ? "Sizin rəyiniz"
                              : `İstifadəçi #${value.userId}`}
                          </strong>
                          <PeekRating
                            value={value.rating}
                            readOnly
                            size={16}
                            lift={0}
                            idleColor="var(--line)"
                          />
                        </div>
                        <p>{value.comment || "Şərh əlavə edilməyib."}</p>
                        <div className="card-footer">
                          <span className="meta">{date(value.createdAt)}</span>
                          {value.userId === user.id && (
                            <div className="actions">
                              <IconButton
                                label="Rəyi dəyiş"
                                onClick={() => setReview(value)}
                              >
                                <Pencil size={15} />
                              </IconButton>
                              <IconButton
                                label="Rəyi sil"
                                onClick={() =>
                                  confirm({
                                    title: "Rəyi silmək istəyirsiniz?",
                                    action: async () => {
                                      await api(`/reviews/${value.id}`, {
                                        method: "DELETE",
                                      });
                                      extras.reload();
                                      state.reload();
                                      toast("Rəy silindi.");
                                    },
                                  })
                                }
                              >
                                <Trash2 size={15} />
                              </IconButton>
                            </div>
                          )}
                        </div>
                      </article>
                    ))}
                  </div>
                ) : (
                  <div className="catalog-grid">
                    {extras.data.map((product) => (
                      <ProductCard product={product} key={product.id} />
                    ))}
                  </div>
                )
              ) : (
                <Empty
                  title={
                    mechanic ? "İlk rəyi siz yazın" : "Hazırda məhsul yoxdur"
                  }
                  description={
                    mechanic
                      ? "Təcrübənizi bölüşərək başqalarına kömək edin."
                      : "Mağazanın əlavə etdiyi məhsullar burada görünəcək."
                  }
                />
              )}
            </Resource>
            {appointment && (
              <AppointmentEditor
                mechanicId={item.id}
                onClose={() => setAppointment(false)}
                onDone={() =>
                  toast(
                    "Görüş yaradıldı. Görüşlərim bölməsində izləyə bilərsiniz.",
                  )
                }
              />
            )}
            {review && (
              <Editor
                title={review.id ? "Rəyi dəyiş" : "Təcrübənizi bölüşün"}
                initial={review}
                fields={[
                  {
                    key: "rating",
                    label: "Qiymət",
                    type: "rating",
                    wide: true,
                    default: 5,
                    required: true,
                  },
                  {
                    key: "comment",
                    label: "Rəyiniz",
                    type: "textarea",
                    wide: true,
                    maxLength: 500,
                  },
                ]}
                onClose={() => setReview(null)}
                onSave={async (values) => {
                  await api(review.id ? `/reviews/${review.id}` : "/reviews", {
                    method: review.id ? "PUT" : "POST",
                    body: {
                      ...values,
                      rating: Number(values.rating),
                      ...(!review.id ? { mechanicId: item.id } : {}),
                    },
                  });
                  setReview(null);
                  extras.reload();
                  state.reload();
                  toast("Rəy yadda saxlanıldı.");
                }}
              />
            )}
          </>
        )}
      </Resource>
    </Page>
  );
}
export function ProductDetail({ slug }) {
  const state = useResource(`/products/${encodeURIComponent(slug)}`);
  const { addCart, act } = useApp();
  const stores = useResource("/stores");
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
                      const result = await api(`/chat/store/${item.storeId}`, {
                        method: "POST",
                      });
                      navigate(`/chat/${result.id}`);
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
export function Cart() {
  const { cart, setCart, toast } = useApp();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const total = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
  const update = (id, quantity) =>
    setCart((current) =>
      current.map((item) =>
        item.id === id
          ? {
              ...item,
              quantity: Math.max(1, Math.min(item.stock, 10000, quantity)),
            }
          : item,
      ),
    );
  return (
    <Page
      title="Səbətiniz"
      description="Bir sifariş bir mağazanın məhsullarından ibarətdir."
    >
      {cart.length ? (
        <div className="cart-layout">
          <div className="card rows-card">
            {cart.map((item) => (
              <div className="cart-row" key={item.id}>
                <Thumbnail src={item.imageUrl} icon={Package} />
                <div className="record-info">
                  <strong>{item.name}</strong>
                  <span className="meta">{money(item.price)} / ədəd</span>
                </div>
                <div className="quantity">
                  <IconButton
                    label="Sayını azalt"
                    disabled={busy || item.quantity === 1}
                    onClick={() => update(item.id, item.quantity - 1)}
                  >
                    <Minus size={14} />
                  </IconButton>
                  <input
                    aria-label={`${item.name} sayı`}
                    type="number"
                    min={1}
                    max={Math.min(item.stock, 10000)}
                    value={item.quantity}
                    disabled={busy}
                    onChange={(event) =>
                      update(item.id, Number(event.target.value))
                    }
                  />
                  <IconButton
                    label="Sayını artır"
                    disabled={
                      busy ||
                      item.quantity >= item.stock ||
                      item.quantity >= 10000
                    }
                    onClick={() => update(item.id, item.quantity + 1)}
                  >
                    <Plus size={14} />
                  </IconButton>
                </div>
                <strong>{money(item.price * item.quantity)}</strong>
                <IconButton
                  label="Məhsulu səbətdən çıxar"
                  disabled={busy}
                  onClick={() =>
                    setCart((current) =>
                      current.filter((value) => value.id !== item.id),
                    )
                  }
                >
                  <Trash2 size={16} />
                </IconButton>
              </div>
            ))}
          </div>
          <aside className="card checkout-card">
            <h2>Sifarişin xülasəsi</h2>
            <div className="summary-row">
              <span>Məhsul sayı</span>
              <strong>
                {cart.reduce((sum, item) => sum + item.quantity, 0)}
              </strong>
            </div>
            <div className="summary-row">
              <span>Ümumi məbləğ</span>
              <strong>{money(total)}</strong>
            </div>
            <p className="meta">
              Yekun qiymət və stok sifariş yaradılarkən mağazanın cari
              məlumatlarına görə yoxlanılır.
            </p>
            {error && (
              <p role="alert" className="form-error">
                {error}
              </p>
            )}
            <Button
              className="full-width"
              busy={busy}
              onClick={async () => {
                setBusy(true);
                setError("");
                try {
                  await api("/orders", {
                    method: "POST",
                    body: {
                      items: cart.map((item) => ({
                        productId: item.id,
                        quantity: item.quantity,
                      })),
                    },
                  });
                  setCart([]);
                  toast("Sifariş yaradıldı.");
                  navigate("/orders");
                } catch (failure) {
                  setError(failure.message);
                } finally {
                  setBusy(false);
                }
              }}
            >
              Sifarişi təsdiqlə <ArrowRight size={16} />
            </Button>
          </aside>
        </div>
      ) : (
        <Empty
          icon={ShoppingBag}
          title="Səbətiniz hələ boşdur"
          description="Növbəti yol üçün lazım olanları kataloqdan seçin."
          action={
            <a href="#/products" className="button primary">
              Məhsullara bax
            </a>
          }
        />
      )}
    </Page>
  );
}
