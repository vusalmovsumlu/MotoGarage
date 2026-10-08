import { useState } from "react";

export function Thumbnail({ src, icon: Icon, alt = "" }) {
  const [failedSrc, setFailedSrc] = useState(null);
  return (
    <div className="thumbnail">
      {src && src !== failedSrc ? (
        <img src={src} alt={alt} onError={() => setFailedSrc(src)} />
      ) : (
        Icon && <Icon size={30} strokeWidth={1.5} />
      )}
    </div>
  );
}
