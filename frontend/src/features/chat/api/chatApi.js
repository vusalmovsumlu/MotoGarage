import { api } from "../../../shared/api/client.js";

export const chatApi = {
  conversationsPath: () => "/chat/conversations",
  messagesPath: (id) => `/chat/conversations/${id}/messages`,
  start: (type, id) => api(`/chat/${type}/${id}`, { method: "POST" }),
  read: (id, signal) =>
    api(`/chat/conversations/${id}/read`, { method: "PATCH", signal }),
  send: (id, content) =>
    api(`/chat/conversations/${id}/messages`, {
      method: "POST",
      body: { content },
    }),
};
