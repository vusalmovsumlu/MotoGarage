import { Button } from "../../../components/ui/Button.jsx";
import { Send, Paperclip } from "lucide-react";
import { AttachmentPreview } from "./AttachmentPreview.jsx";

export function MessageComposer({
  send,
  content,
  sending,
  messages,
  setContent,
  media,
}) {
  return (
    <form className="message-composer message-composer--media" onSubmit={send}>
      <AttachmentPreview media={media} disabled={sending} />
      <div className="message-composer-inputs">
        <label className="chat-file-picker" title="Şəkil və ya video əlavə et">
          <Paperclip size={19} />
          <input
            type="file"
            aria-label="Şəkil və ya video əlavə et"
            accept="image/jpeg,image/png,image/webp,video/mp4,video/webm"
            disabled={
              sending ||
              media.busy ||
              Boolean(media.attachment) ||
              messages.loading ||
              Boolean(messages.error)
            }
            onChange={media.choose}
          />
        </label>
        <textarea
          aria-label="Mesajınız"
          placeholder="Mesajınızı yazın…"
          value={content}
          maxLength={2000}
          rows={2}
          disabled={
            sending || media.busy || messages.loading || Boolean(messages.error)
          }
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
            (!content.trim() && !media.attachment) ||
            media.busy ||
            messages.loading ||
            Boolean(messages.error)
          }
        >
          <Send size={17} />
          <span>Göndər</span>
        </Button>
      </div>
    </form>
  );
}
