import { useCallback, useEffect, useState } from "react";
import { api } from "./api";

export function useResource(path) {
  const [state, setState] = useState({
    data: null,
    error: null,
    path: null,
    version: -1,
  });
  const [version, setVersion] = useState(0);
  const reload = useCallback(() => setVersion((value) => value + 1), []);
  useEffect(() => {
    const controller = new AbortController();
    if (!path) return () => controller.abort();
    api(path, { signal: controller.signal })
      .then((data) => {
        if (!controller.signal.aborted)
          setState({ data, error: null, path, version });
      })
      .catch((error) => {
        if (!controller.signal.aborted)
          setState({ data: null, error, path, version });
      });
    return () => controller.abort();
  }, [path, version]);
  const current = path && state.path === path && state.version === version;
  return {
    data: current ? state.data : null,
    error: current ? state.error : null,
    loading: Boolean(path) && !current,
    reload,
  };
}
export function useRoute() {
  const [route, setRoute] = useState(
    () => window.location.hash.slice(1) || "/home",
  );
  useEffect(() => {
    const change = () => {
      setRoute(window.location.hash.slice(1) || "/home");
      window.scrollTo(0, 0);
    };
    window.addEventListener("hashchange", change);
    return () => window.removeEventListener("hashchange", change);
  }, []);
  return route;
}
export const navigate = (path) => {
  window.location.hash = path;
};
