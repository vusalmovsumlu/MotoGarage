import { useRoute, navigate } from "./useRoute.js";
import { useApp } from "../../shared/hooks/useApp.js";
import { useEffect } from "react";
import { Workspace } from "../../components/layout/Workspace.jsx";
import { resolveRoute as screen } from "./resolveRoute.jsx";
import Auth from "../../features/auth/pages/Auth.jsx";

export function AppRouter() {
  const route = useRoute();
  const { user, theme, toggleTheme, cart, logout, setNotificationReload } =
    useApp();
  const authenticated = Boolean(user);
  useEffect(() => {
    if (!authenticated && !["/login", "/register", "/verify"].includes(route))
      navigate("/login");
    if (authenticated && ["/login", "/register", "/verify"].includes(route))
      navigate("/home");
  }, [authenticated, route]);

  return user ? (
    <Workspace
      user={user}
      route={route}
      theme={theme}
      toggleTheme={toggleTheme}
      cartCount={cart.reduce((sum, item) => sum + item.quantity, 0)}
      onLogout={logout}
      setNotificationReload={setNotificationReload}
    >
      {screen(route, user)}
    </Workspace>
  ) : (
    <Auth key={route} route={route} />
  );
}
