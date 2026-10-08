import { useResource } from "../../shared/hooks/useResource.js";
import { notificationsApi } from "../../features/notifications/api/notificationsApi.js";
import { useState, useEffect } from "react";
import {
  CalendarDays,
  ClipboardList,
  ShoppingBag,
  Package,
  Wrench,
  Bell,
  Shield,
  ChevronDown,
  MessageCircle,
  Sun,
  Moon,
  UserRound,
  LogOut,
} from "lucide-react";
import { links } from "../../app/router/navigation.js";
import { IconButton } from "../ui/IconButton.jsx";
import GooeyNav from "../ui/GooeyNav.jsx";
import { roleNames } from "../../shared/constants/labels.js";
import Footer from "./Footer.jsx";

export function Workspace({
  user,
  route,
  theme,
  toggleTheme,
  children,
  onLogout,
  cartCount,
  setNotificationReload,
}) {
  const notifications = useResource(notificationsApi.unreadCountPath());
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
          <GooeyNav
            items={[...links, ...(roleLink ? [roleLink] : [])]}
            path={path}
          >
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
          </GooeyNav>
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
      <Footer year={year} />
    </>
  );
}
