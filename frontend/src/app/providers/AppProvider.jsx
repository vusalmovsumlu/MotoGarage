import { useAuthSession } from "../../features/auth/hooks/useAuthSession.js";
import { useTheme } from "../../shared/hooks/useTheme.js";
import { useRef, useCallback } from "react";
import { useFeedback } from "../../shared/hooks/useFeedback.js";
import { useCart } from "../../features/orders/hooks/useCart.js";
import { authApi } from "../../features/auth/api/authApi.js";
import { getSession, setSession } from "../../shared/api/client.js";
import { navigate } from "../router/useRoute.js";
import { AppContext } from "./AppContext.js";
import { ToastRegion } from "../../components/feedback/ToastRegion.jsx";
import { ConfirmDialog } from "../../components/feedback/ConfirmDialog.jsx";

export function AppProvider({ children }) {
  const { user, setOverride } = useAuthSession();
  const { theme, toggleTheme } = useTheme();
  const notificationRef = useRef(() => {});
  const setNotificationReload = useCallback((fn) => {
    notificationRef.current = fn;
  }, []);
  const notificationReload = useCallback(() => notificationRef.current(), []);

  const { notices, setNotices, confirmation, setConfirmation, toast, act } =
    useFeedback(notificationReload);
  const { cart, setCart, addCart } = useCart(user, toast);
  async function logout() {
    await act(async () => {
      try {
        await authApi.logout(getSession()?.refreshToken);
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
    toggleTheme,
    notificationReload,
    updateUser: (values) => setOverride({ id: user.id, ...values }),
    logout,
    setNotificationReload,
  };
  return (
    <AppContext.Provider value={context}>
      {children}
      <ToastRegion notices={notices} setNotices={setNotices} />
      {confirmation && confirmation.ownerId === user?.id && (
        <ConfirmDialog
          value={confirmation}
          onClose={() => setConfirmation(null)}
        />
      )}
    </AppContext.Provider>
  );
}
