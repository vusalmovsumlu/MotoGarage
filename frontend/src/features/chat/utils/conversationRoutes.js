export function conversationPath(conversation) {
  return `/chat/${conversation.slug || conversation.id}`;
}
