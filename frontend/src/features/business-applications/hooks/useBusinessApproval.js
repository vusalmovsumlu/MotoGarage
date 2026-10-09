import { useCallback, useEffect, useState } from "react";
import { api } from "../../../shared/api/client.js";
import { businessApplicationsApi } from "../api/businessApplicationsApi.js";

export function useBusinessApproval(required) {
  const [state, setState] = useState({
    data: null,
    error: null,
    loading: true,
  });
  const [version, setVersion] = useState(0);
  const reload = useCallback(() => setVersion((value) => value + 1), []);
  const approved = state.data?.status === "Approved";
  useEffect(() => {
    if (!required || approved) return;
    const controller = new AbortController();
    api(businessApplicationsApi.minePath(), { signal: controller.signal })
      .then((data) => {
        if (!controller.signal.aborted)
          setState({ data, error: null, loading: false });
      })
      .catch((error) => {
        if (!controller.signal.aborted)
          setState((previous) => ({ ...previous, error, loading: false }));
      });
    return () => controller.abort();
  }, [required, approved, version]);
  useEffect(() => {
    if (!required || approved) return;
    const timer = setInterval(() => {
      if (!document.hidden) reload();
    }, 30000);
    return () => clearInterval(timer);
  }, [required, approved, reload]);
  return { ...state, reload, approved };
}
