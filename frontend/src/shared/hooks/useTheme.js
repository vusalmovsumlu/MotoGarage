import { useState, useEffect } from "react";

export function useTheme() {
  const [theme, setTheme] = useState(() => {
    try {
      return (
        localStorage.getItem("motogarage.theme") ||
        (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light")
      );
    } catch {
      return "light";
    }
  });
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    try {
      localStorage.setItem("motogarage.theme", theme);
    } catch {
      /* Theme still works without storage. */
    }
  }, [theme]);
  return {
    theme,
    toggleTheme: () =>
      setTheme((current) => (current === "dark" ? "light" : "dark")),
  };
}
