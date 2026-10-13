import { useEffect, useRef, useState } from "react";
import { chatApi } from "../api/chatApi.js";

export function useChatAttachment(conversationId) {
  const [attachment, setAttachment] = useState(null);
  const [file, setFile] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const request = useRef(null);
  useEffect(() => () => request.current?.abort(), []);
  const choose = async (event) => {
    const selected = event.target.files?.[0];
    event.target.value = "";
    if (!selected || !conversationId || busy || attachment) return;
    const image =
      /^image\/(jpeg|png|webp)$/.test(selected.type) &&
      /\.(jpe?g|png|webp)$/i.test(selected.name);
    const video =
      /^video\/(mp4|webm)$/.test(selected.type) &&
      /\.(mp4|webm)$/i.test(selected.name);
    if (
      (!image && !video) ||
      !selected.size ||
      selected.size > (image ? 5 : 25) * 1024 * 1024
    ) {
      setError(
        "JPG, PNG, WEBP şəkli (5 MB) və ya MP4, WEBM videosu (25 MB) seçin.",
      );
      return;
    }
    setError("");
    setBusy(true);
    setFile(selected);
    const controller = new AbortController();
    request.current = controller;
    try {
      setAttachment(
        await chatApi.upload(conversationId, selected, controller.signal),
      );
    } catch (failure) {
      if (failure.name !== "AbortError") {
        setError(failure.message);
        setFile(null);
      }
    } finally {
      if (!controller.signal.aborted) setBusy(false);
    }
  };
  const remove = async () => {
    if (!attachment || busy) return;
    setBusy(true);
    setError("");
    try {
      await chatApi.removeAttachment(conversationId, attachment.id);
      setAttachment(null);
      setFile(null);
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  };
  const sent = () => {
    setAttachment(null);
    setFile(null);
    setError("");
  };
  return { attachment, file, busy, error, choose, remove, sent };
}
