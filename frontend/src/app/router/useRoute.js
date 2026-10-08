import { useState, useEffect } from "react";

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

export const replaceRoute = (path) => {
  window.history.replaceState(null, "", `#${path}`);
  window.dispatchEvent(new HashChangeEvent("hashchange"));
};
