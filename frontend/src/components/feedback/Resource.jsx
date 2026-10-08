import ResourceSkeleton from "./Skeleton.jsx";

import { Button } from "../ui/Button.jsx";

import { RefreshCw } from "lucide-react";

export function Resource({
  state,
  children,
  allow404 = false,
  skeleton = "list",
}) {
  if (state.loading) return <ResourceSkeleton variant={skeleton} />;
  if (state.error && !(allow404 && state.error.status === 404))
    return (
      <div className="error-panel" role="alert">
        <h3>Məlumatlar yüklənmədi</h3>
        <p>{state.error.message}</p>
        <Button variant="secondary" onClick={state.reload}>
          <RefreshCw size={16} /> Yenidən cəhd et
        </Button>
      </div>
    );
  return children;
}
