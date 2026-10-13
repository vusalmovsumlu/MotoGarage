import { useSyncExternalStore, useEffect, useRef } from "react";
import {
  getHashRoute,
  subscribeHashRoute,
} from "../../shared/utils/routeQuery.js";

export function useRoute() {
  const route = useSyncExternalStore(subscribeHashRoute, getHashRoute);
  const previousPath = useRef(route.split("?")[0]);
  useEffect(() => {
    const path = route.split("?")[0];
    if (previousPath.current !== path) window.scrollTo(0, 0);
    previousPath.current = path;
  }, [route]);
  return route;
}
export {
  navigateHashRoute as navigate,
  replaceHashRoute as replaceRoute,
} from "../../shared/utils/routeQuery.js";
