import { Button } from "../../../components/ui/Button.jsx";
import { Send } from "lucide-react";

export function MessageComposer({
  send,
  content,
  sending,
  messages,
  setContent,
}) {
  return (
    <form className="message-composer" onSubmit={send}>
      <textarea
        aria-label="Mesajınız"
        placeholder="Mesajınızı yazın…"
        value={content}
        maxLength={2000}
        rows={2}
        disabled={sending || messages.loading || Boolean(messages.error)}
        onChange={(event) => setContent(event.target.value)}
        onKeyDown={(event) => {
          if (
            event.key === "Enter" &&
            !event.shiftKey &&
            !event.nativeEvent.isComposing
          ) {
            event.preventDefault();
            event.currentTarget.form.requestSubmit();
          }
        }}
      />
      <Button
        type="submit"
        busy={sending}
        disabled={
          !content.trim() || messages.loading || Boolean(messages.error)
        }
      >
        <Send size={17} />
        <span>Göndər</span>
      </Button>
    </form>
  );
}
