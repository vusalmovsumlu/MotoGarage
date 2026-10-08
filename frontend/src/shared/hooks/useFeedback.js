import { useState, useRef, useCallback, useEffect } from "react";

export function useFeedback(notificationReload) {
  const [notices, setNotices] = useState([]);
  const [confirmation, setConfirmation] = useState(null);
  const timers = useRef(new Set());
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
  return { notices, setNotices, confirmation, setConfirmation, toast, act };
}
