import { useEffect, useRef, useState } from "react";
import { Film, Image as ImageIcon, RotateCw } from "lucide-react";
import { chatApi } from "../api/chatApi.js";

export function ChatMedia({ attachment, conversationId }) {
  const [visible, setVisible] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState({ url: null, error: "" });
  const box = useRef(null);
  const video = attachment.kind === "video";
  useEffect(() => {
    if (video) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: "100px" },
    );
    observer.observe(box.current);
    return () => observer.disconnect();
  }, [video]);
  useEffect(() => {
    if (!visible) return;
    const controller = new AbortController();
    let url;
    chatApi
      .attachmentContent(conversationId, attachment.id, controller.signal)
      .then((blob) => {
        if (controller.signal.aborted) return;
        url = URL.createObjectURL(blob);
        setState({ url, error: "" });
      })
      .catch((failure) => {
        if (!controller.signal.aborted)
          setState({ url: null, error: failure.message });
      });
    return () => {
      controller.abort();
      if (url) URL.revokeObjectURL(url);
    };
  }, [conversationId, attachment.id, visible, attempt]);
  return (
    <div className="chat-media" ref={box}>
      {state.url ? (
        video ? (
          <video
            src={state.url}
            controls
            playsInline
            preload="metadata"
            aria-label={attachment.fileName}
          />
        ) : (
          <a href={state.url} target="_blank" rel="noreferrer">
            <img src={state.url} alt={attachment.fileName} />
          </a>
        )
      ) : state.error ? (
        <div role="alert">
          <p>{state.error}</p>
          <button
            type="button"
            onClick={() => {
              setState({ url: null, error: "" });
              setAttempt((value) => value + 1);
            }}
          >
            <RotateCw size={15} /> Yenidən yoxla
          </button>
        </div>
      ) : video && !visible ? (
        <button
          type="button"
          className="chat-video-open"
          onClick={() => setVisible(true)}
        >
          <Film size={22} /> Videonu aç
        </button>
      ) : (
        <div className="chat-media-loading" role="status">
          {video ? <Film size={22} /> : <ImageIcon size={22} />} Fayl yüklənir…
        </div>
      )}
      <small>
        {attachment.fileName} ·{" "}
        {(attachment.sizeBytes / (1024 * 1024)).toFixed(1)} MB
      </small>
    </div>
  );
}
