export const getHashRoute = () => window.location.hash.slice(1) || "/home";

export function subscribeHashRoute(listener) {
  window.addEventListener("hashchange", listener);
  window.addEventListener("popstate", listener);
  return () => {
    window.removeEventListener("hashchange", listener);
    window.removeEventListener("popstate", listener);
  };
}

export function routeParams(route = getHashRoute()) {
  const index = route.indexOf("?");
  return new URLSearchParams(index < 0 ? "" : route.slice(index + 1));
}

export const navigateHashRoute = (path) => {
  window.location.hash = path;
};
export function replaceHashRoute(path) {
  window.history.replaceState(window.history.state, "", `#${path}`);
  window.dispatchEvent(new HashChangeEvent("hashchange"));
}

export function updateRouteQuery(
  values,
  { defaults = {}, remove = [], path, replace = false } = {},
) {
  const current = getHashRoute();
  const params = routeParams(current);
  remove.forEach((key) => params.delete(key));
  for (const [key, value] of Object.entries(values)) {
    const normalized =
      value === null || value === undefined ? "" : String(value).trim();
    if (
      normalized === "" ||
      (key in defaults && normalized === String(defaults[key]))
    )
      params.delete(key);
    else params.set(key, normalized);
  }
  const query = params.toString();
  const next = `${path || current.split("?")[0]}${query ? `?${query}` : ""}`;
  if (next !== current) (replace ? replaceHashRoute : navigateHashRoute)(next);
  return next;
}

export const queryText = (params, key) => params.get(key)?.trim() || "";
export function queryChoice(params, key, choices, fallback) {
  return (
    choices.find(
      (value) =>
        String(value).toLowerCase() === queryText(params, key).toLowerCase(),
    ) ?? fallback
  );
}
export function queryPage(params) {
  const raw = queryText(params, "page");
  const value = Number(raw);
  return /^\d+$/.test(raw) &&
    Number.isSafeInteger(value) &&
    value >= 1 &&
    value <= 100000
    ? value
    : 1;
}
export function queryPrice(params, key) {
  const raw = queryText(params, key);
  return /^\d+(?:\.\d{1,2})?$/.test(raw) &&
    Number.isFinite(Number(raw)) &&
    Number(raw) <= 1e12
    ? raw
    : "";
}
export function isQueryDate(value) {
  if (
    !/^\d{4}-\d{2}-\d{2}$/.test(value) ||
    value < "0001-01-01" ||
    value > "9999-12-24"
  )
    return false;
  const parsed = new Date(`${value}T00:00:00Z`);
  return (
    !Number.isNaN(parsed.getTime()) &&
    parsed.toISOString().slice(0, 10) === value
  );
}
export function queryDateRange(
  params,
  startKey,
  endKey,
  fallback,
  maxDays = Infinity,
) {
  const start = queryText(params, startKey),
    end = queryText(params, endKey);
  if (
    !isQueryDate(start) ||
    !isQueryDate(end) ||
    start > end ||
    (new Date(end) - new Date(start)) / 86400000 >= maxDays
  )
    return fallback;
  return { [startKey]: start, [endKey]: end };
}
