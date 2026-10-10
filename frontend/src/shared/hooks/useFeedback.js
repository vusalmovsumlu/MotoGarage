import { useState, useCallback } from "react";

export function useFeedback(notificationReload) {
  const [notices, setNotices] = useState([]);
  const [confirmation, setConfirmation] = useState(null);
  const toast = useCallback(
    (message, error = false) => {
      const id = crypto.randomUUID();
      setNotices((current) => [...current.slice(-2), { id, message, error }]);
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
  return { notices, setNotices, confirmation, setConfirmation, toast, act };
}
