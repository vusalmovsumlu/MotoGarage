export const API_ROOT = (import.meta.env.VITE_API_URL || "/api").replace(
  /\/$/,
  "",
);
export const HUB_URL =
  import.meta.env.VITE_HUB_URL ||
  (API_ROOT.startsWith("http")
    ? new URL("/hubs/chat", API_ROOT).href
    : "/hubs/chat");
