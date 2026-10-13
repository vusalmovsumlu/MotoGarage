import { api } from "../../../shared/api/client.js";
import { conversationPath } from "../utils/conversationRoutes.js";

export const chatApi = {
  path: conversationPath,
  conversationsPath: () => "/chat/conversations",
  messagesPath: (id) => `/chat/conversations/${id}/messages`,
  start: (type, id) => api(`/chat/${type}/${id}`, { method: "POST" }),
  read: (id, signal) =>
    api(`/chat/conversations/${id}/read`, { method: "PATCH", signal }),
  upload: (id, file, signal) => {
    const form = new FormData();
    form.append("file", file);
    return api(`/chat/conversations/${id}/attachments`, {
      method: "POST",
      body: form,
      signal,
    });
  },
  removeAttachment: (id, attachmentId) =>
    api(`/chat/conversations/${id}/attachments/${attachmentId}`, {
      method: "DELETE",
    }),
  attachmentContent: (id, attachmentId, signal) =>
    api(`/chat/conversations/${id}/attachments/${attachmentId}/content`, {
      signal,
      responseType: "blob",
    }),
  send: (id, content, attachmentId) =>
    api(`/chat/conversations/${id}/messages`, {
      method: "POST",
      body: { content, ...(attachmentId ? { attachmentId } : {}) },
    }),
};
