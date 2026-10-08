import { api } from "./client.js";
export function uploadImage(form) {
  return api("/images/upload", { method: "POST", body: form });
}
