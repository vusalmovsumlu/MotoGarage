import { useMemo, useSyncExternalStore } from "react";
import {
  getHashRoute,
  subscribeHashRoute,
  routeParams,
  updateRouteQuery,
} from "../utils/routeQuery.js";

export function useUrlQuery() {
  const route = useSyncExternalStore(subscribeHashRoute, getHashRoute);
  const params = useMemo(() => routeParams(route), [route]);
  return { route, params, update: updateRouteQuery };
}
