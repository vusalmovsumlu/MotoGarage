import {
  useCallback,
  useEffect,
  useRef,
  useState,
  useSyncExternalStore,
} from "react";
import {
  Bell,
  CalendarDays,
  CarFront,
  ChevronDown,
  ClipboardList,
  Home as HomeIcon,
  LogOut,
  MessageCircle,
  Moon,
  Package,
  Shield,
  ShoppingBag,
  Sun,
  UserRound,
  Wrench,
  X,
} from "lucide-react";
import { AppContext } from "./context";
import {
  api,
  getSession,
  sessionUser,
  setSession,
  subscribeSession,
} from "./lib/api";
import { navigate, useResource, useRoute } from "./lib/hooks";
import { roleNames } from "./lib/format";
import { ConfirmDialog, Empty, IconButton, Page } from "./components/ui";
import Auth from "./pages/Auth";
import Home from "./pages/Home";
import { Records, VehicleDetail, Vehicles } from "./pages/Garage";
import { Catalog, Cart, MerchantDetail, ProductDetail } from "./pages/Catalog";
import { BusinessProfile, Inventory } from "./pages/Business";
import { WorkList } from "./pages/Work";
import { AdminUsers, Notifications, Profile } from "./pages/Account";
import Chat from "./pages/Chat";

const links = [
  { path: "/home", title: "İş masası", icon: HomeIcon },
  { path: "/vehicles", title: "Qarajım", icon: CarFront },
  { path: "/mechanics", title: "Ustalar", icon: Wrench },
  { path: "/products", title: "Məhsullar", icon: ShoppingBag },
];
function screen(route, user) {
  const path = route.split("?")[0];
  const parts = path.split("/").filter(Boolean);
  if (path === "/home") return <Home />;
  if (path === "/vehicles") return <Vehicles />;
  if (parts[0] === "vehicles" && /^\d+$/.test(parts[1] || ""))
    return <VehicleDetail id={parts[1]} />;
  if (
    parts[0] === "records" &&
    ["fuel", "expenses", "service", "reminders"].includes(parts[1])
  )
    return <Records key={route} kind={parts[1]} route={route} />;
  if (
    ["mechanics", "stores", "products"].includes(parts[0]) &&
    parts.length === 1
  )
    return <Catalog key={parts[0]} type={parts[0]} />;
  if (["mechanics", "stores"].includes(parts[0]) && parts[1])
    return <MerchantDetail key={path} type={parts[0]} slug={parts[1]} />;
  if (parts[0] === "products" && parts[1])
    return <ProductDetail key={path} slug={parts[1]} />;
  if (path === "/cart") return <Cart />;
  if (path === "/orders" || path === "/appointments")
    return <WorkList key={path} type={parts[0]} />;
  if (path === "/profile") return <Profile />;
  if (path === "/notifications") return <Notifications />;
  if (parts[0] === "chat" && (!parts[1] || /^\d+$/.test(parts[1])))
    return <Chat key={path} selectedId={parts[1]} />;
  if (path === "/admin/users" && user.role === "Admin") return <AdminUsers />;
  if (path === "/business" && ["Mechanic", "Store"].includes(user.role))
    return <BusinessProfile />;
  if (path === "/inventory" && user.role === "Store") return <Inventory />;
  if (path === "/work/orders" && user.role === "Store")
    return <WorkList key={path} type="orders" incoming />;
  if (path === "/work/appointments" && user.role === "Mechanic")
    return <WorkList key={path} type="appointments" incoming />;
  return (
    <Page title="Səhifə əlçatan deyil">
      <Empty
        title="Bu səhifə tapılmadı və ya rolunuza uyğun deyil."
        action={
          <a href="#/home" className="button primary">
            İş masasına keç
          </a>
        }
      />
    </Page>
  );
}
function Workspace({
  user,
  route,
  theme,
  toggleTheme,
  children,
  onLogout,
  cartCount,
  setNotificationReload,
}) {
  const notifications = useResource("/notifications/unread-count");
  const notificationFetch = notifications.reload;
  const [year] = useState(() => new Date().getFullYear());
  useEffect(() => {
    setNotificationReload(notificationFetch);
    const interval = setInterval(() => {
      if (!document.hidden) notificationFetch();
    }, 30000);
    return () => clearInterval(interval);
  }, [notificationFetch, setNotificationReload]);
  const moreLinks = [
    { path: "/appointments", title: "Görüşlərim", icon: CalendarDays },
    { path: "/orders", title: "Sifarişlərim", icon: ClipboardList },
    { path: "/stores", title: "Mağazalar", icon: ShoppingBag },
    { path: "/records/fuel", title: "Yanacaq", icon: Package },
    { path: "/records/expenses", title: "Xərclər", icon: ClipboardList },
    { path: "/records/service", title: "Servis tarixçəsi", icon: Wrench },
    { path: "/records/reminders", title: "Xatırlatmalar", icon: Bell },
  ];
  const roleLink =
    user.role === "Admin"
      ? { path: "/admin/users", title: "İstifadəçilər", icon: Shield }
      : user.role === "Store"
        ? { path: "/inventory", title: "Məhsullarım", icon: Package }
        : user.role === "Mechanic"
          ? {
              path: "/work/appointments",
              title: "Gələn görüşlər",
              icon: CalendarDays,
            }
          : null;
  const path = route.split("?")[0];
  return (
    <>
      <a
        className="skip-link"
        href="#main-content"
        onClick={(event) => {
          event.preventDefault();
          document.getElementById("main-content").focus();
        }}
      >
        Əsas məzmuna keç
      </a>
      <header className="site-header">
        <div className="header-inner">
          <a href="#/home" className="brand">
            <span className="brand-symbol">
              <Wrench size={17} />
            </span>
            MotoGarage
          </a>
          <nav className="main-nav" aria-label="Əsas menyu">
            {[...links, ...(roleLink ? [roleLink] : [])].map((item) => (
              <a
                href={`#${item.path}`}
                className={
                  path === item.path ||
                  (item.path !== "/home" && path.startsWith(`${item.path}/`))
                    ? "active"
                    : ""
                }
                aria-current={path === item.path ? "page" : undefined}
                key={item.path}
              >
                <item.icon size={15} />
                {item.title}
              </a>
            ))}
            <details className="nav-more">
              <summary>
                Digər <ChevronDown size={13} />
              </summary>
              <div className="dropdown">
                {moreLinks.map((item) => (
                  <a
                    href={`#${item.path}`}
                    key={item.path}
                    onClick={(event) =>
                      event.currentTarget
                        .closest("details")
                        .removeAttribute("open")
                    }
                  >
                    <item.icon size={15} />
                    {item.title}
                  </a>
                ))}
                {["Mechanic", "Store"].includes(user.role) && (
                  <>
                    <a
                      href="#/business"
                      onClick={(event) =>
                        event.currentTarget
                          .closest("details")
                          .removeAttribute("open")
                      }
                    >
                      <Wrench size={15} />
                      Biznes profilim
                    </a>
                    {user.role === "Store" && (
                      <a
                        href="#/work/orders"
                        onClick={(event) =>
                          event.currentTarget
                            .closest("details")
                            .removeAttribute("open")
                        }
                      >
                        <Package size={15} />
                        Gələn sifarişlər
                      </a>
                    )}
                  </>
                )}
              </div>
            </details>
          </nav>
          <div className="header-actions">
            <a
              className={`icon-button ${path.startsWith("/chat") ? "selected" : ""}`}
              href="#/chat"
              aria-label="Söhbətlər"
            >
              <MessageCircle size={19} />
            </a>
            <a
              className="icon-button count-button"
              href="#/cart"
              aria-label={`Səbət, ${cartCount} məhsul`}
            >
              <ShoppingBag size={19} />
              {cartCount > 0 && <span className="count-dot">{cartCount}</span>}
            </a>
            <a
              className="icon-button count-button"
              href="#/notifications"
              aria-label={`Bildirişlər, ${notifications.data?.unreadCount || 0} oxunmamış`}
            >
              <Bell size={19} />
              {notifications.data?.unreadCount > 0 && (
                <span className="count-dot">
                  {notifications.data.unreadCount > 99
                    ? "99+"
                    : notifications.data.unreadCount}
                </span>
              )}
            </a>
            <IconButton label="Temanı dəyiş" onClick={toggleTheme}>
              {theme === "dark" ? <Sun size={18} /> : <Moon size={18} />}
            </IconButton>
            <details className="account-menu">
              <summary aria-label="Hesab menyusu">
                <span className="avatar">{user.fullName[0]}</span>
                <ChevronDown size={12} />
              </summary>
              <div className="dropdown">
                <div className="account-info">
                  <strong>{user.fullName}</strong>
                  <span className="meta">{roleNames[user.role]}</span>
                </div>
                <a
                  href="#/profile"
                  onClick={(event) =>
                    event.currentTarget
                      .closest("details")
                      .removeAttribute("open")
                  }
                >
                  <UserRound size={15} />
                  Profil
                </a>
                <button onClick={onLogout}>
                  <LogOut size={15} />
                  Çıxış et
                </button>
              </div>
            </details>
          </div>
        </div>
      </header>
      <main id="main-content" tabIndex={-1} className="main-container">
        {children}
      </main>
      <footer className="site-footer">
        <span>MotoGarage</span>
        <span>Avtomobil və motosikletinizin qayğısına qalırıq.</span>
        <span>© {year}</span>
      </footer>
    </>
  );
}
export default function App() {
  const route = useRoute();
  const session = useSyncExternalStore(subscribeSession, getSession);
  const [override, setOverride] = useState(null);
  const user = session
    ? {
        ...sessionUser(session),
        ...(override?.id === sessionUser(session).id ? override : {}),
      }
    : null;
  const [theme, setTheme] = useState(() => {
    try {
      return (
        localStorage.getItem("motogarage.theme") ||
        (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light")
      );
    } catch {
      return "light";
    }
  });
  const [notices, setNotices] = useState([]);
  const [confirmation, setConfirmation] = useState(null);
  const [cartState, setCartState] = useState({ ownerId: null, items: [] });
  const cart = cartState.ownerId === user?.id ? cartState.items : [];
  const setCart = (update) =>
    setCartState((current) => ({
      ownerId: user?.id,
      items:
        typeof update === "function"
          ? update(current.ownerId === user?.id ? current.items : [])
          : update,
    }));
  const timers = useRef(new Set());
  const notificationRef = useRef(() => {});
  const setNotificationReload = useCallback((fn) => {
    notificationRef.current = fn;
  }, []);
  const notificationReload = useCallback(() => notificationRef.current(), []);
  const toast = useCallback(
    (message, error = false) => {
      const id = crypto.randomUUID();
      setNotices((current) => [...current.slice(-2), { id, message, error }]);
      const timer = setTimeout(() => {
        setNotices((current) => current.filter((item) => item.id !== id));
        timers.current.delete(timer);
      }, 6500);
      timers.current.add(timer);
    },
    [setNotices],
  );
  const act = useCallback(
    async (action, message) => {
      try {
        const result = await action();
        if (message) toast(message);
        notificationReload();
        return result ?? true;
      } catch (failure) {
        toast(failure.message, true);
        return false;
      }
    },
    [toast, notificationReload],
  );
  useEffect(() => () => timers.current.forEach(clearTimeout), []);
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    try {
      localStorage.setItem("motogarage.theme", theme);
    } catch {
      /* Theme still works without storage. */
    }
  }, [theme]);
  const authenticated = Boolean(user);
  useEffect(() => {
    if (!authenticated && !["/login", "/register", "/verify"].includes(route))
      navigate("/login");
    if (authenticated && ["/login", "/register", "/verify"].includes(route))
      navigate("/home");
  }, [authenticated, route]);
  const addCart = (product) => {
    if (cart.length && cart[0].storeId !== product.storeId) {
      toast(
        "Bir sifarişdə yalnız bir mağazanın məhsulları ola bilər. Əvvəl hazırkı səbəti tamamlayın.",
        true,
      );
      return;
    }
    const item = cart.find((value) => value.id === product.id);
    if (item && item.quantity >= Math.min(product.stock, 10000)) {
      toast("Stokda olan saydan çox əlavə edə bilməzsiniz.", true);
      return;
    }
    if (!item && cart.length >= 100) {
      toast(
        "Bir sifariş maksimum 100 fərqli məhsuldan ibarət ola bilər.",
        true,
      );
      return;
    }
    setCart((current) =>
      item
        ? current.map((value) =>
            value.id === product.id
              ? { ...value, quantity: value.quantity + 1 }
              : value,
          )
        : [...current, { ...product, quantity: 1 }],
    );
    toast("Məhsul səbətə əlavə edildi.");
  };
  async function logout() {
    await act(async () => {
      try {
        await api("/auth/logout", {
          method: "POST",
          body: { refreshToken: getSession()?.refreshToken },
        });
      } finally {
        setSession(null);
        setOverride(null);
        navigate("/login");
      }
    });
  }
  const context = {
    user,
    toast,
    act,
    confirm: (value) => setConfirmation({ ...value, ownerId: user.id }),
    cart,
    setCart,
    addCart,
    theme,
    toggleTheme: () =>
      setTheme((current) => (current === "dark" ? "light" : "dark")),
    notificationReload,
    updateUser: (values) => setOverride({ id: user.id, ...values }),
  };
  return (
    <AppContext.Provider value={context}>
      {user ? (
        <Workspace
          user={user}
          route={route}
          theme={theme}
          toggleTheme={context.toggleTheme}
          cartCount={cart.reduce((sum, item) => sum + item.quantity, 0)}
          onLogout={logout}
          setNotificationReload={setNotificationReload}
        >
          {screen(route, user)}
        </Workspace>
      ) : (
        <Auth key={route} route={route} />
      )}
      <div className="toast-region" aria-live="polite" aria-atomic="false">
        {notices.map((item) => (
          <div
            className={`toast ${item.error ? "toast-error" : ""}`}
            key={item.id}
            role={item.error ? "alert" : "status"}
          >
            <span>{item.message}</span>
            <IconButton
              label="Bildirişi bağla"
              onClick={() =>
                setNotices((current) =>
                  current.filter((value) => value.id !== item.id),
                )
              }
            >
              <X size={16} />
            </IconButton>
          </div>
        ))}
      </div>
      {confirmation && confirmation.ownerId === user?.id && (
        <ConfirmDialog
          value={confirmation}
          onClose={() => setConfirmation(null)}
        />
      )}
    </AppContext.Provider>
  );
}
