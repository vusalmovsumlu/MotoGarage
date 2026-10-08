import { useSyncExternalStore, useState } from "react";
import {
  subscribeSession,
  getSession,
  sessionUser,
} from "../../../shared/api/client.js";

export function useAuthSession() {
  const session = useSyncExternalStore(subscribeSession, getSession);
  const [override, setOverride] = useState(null);
  const user = session
    ? {
        ...sessionUser(session),
        ...(override?.id === sessionUser(session).id ? override : {}),
      }
    : null;
  return { user, setOverride };
}
