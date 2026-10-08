// Adapted from React Bits Gooey Nav (David Haz).
// Source: https://reactbits.dev/components/gooey-nav
// License: /public/licenses/react-bits.txt
import { useId, useLayoutEffect, useRef } from "react";
import "./GooeyNav.css";

function isActive(path, itemPath) {
  return (
    path === itemPath ||
    (itemPath === "/vehicles" && path.startsWith("/garage/")) ||
    (itemPath !== "/home" && path.startsWith(`${itemPath}/`))
  );
}

export default function GooeyNav({ items, path, children }) {
  const containerRef = useRef(null);
  const effectRef = useRef(null);
  const timersRef = useRef(new Set());
  const filterId = `gooey-${useId().replace(/[^a-zA-Z0-9-]/g, "")}`;
  const itemsKey = items.map((item) => item.path).join("|");

  useLayoutEffect(() => {
    const container = containerRef.current;
    const effect = effectRef.current;
    const activeLink = container.querySelector('a[aria-current="page"]');
    const update = () => {
      effect.hidden = !activeLink;
      if (!activeLink) return;
      const bounds = container.getBoundingClientRect();
      const target = activeLink.getBoundingClientRect();
      Object.assign(effect.style, {
        left: `${target.left - bounds.left}px`,
        top: `${target.top - bounds.top}px`,
        width: `${target.width}px`,
        height: `${target.height}px`,
      });
    };
    update();
    const observer = new ResizeObserver(update);
    observer.observe(container);
    if (activeLink) observer.observe(activeLink);
    return () => observer.disconnect();
  }, [path, itemsKey]);

  useLayoutEffect(
    () => () => {
      timersRef.current.forEach(clearTimeout);
      timersRef.current.clear();
    },
    [],
  );

  function burst(event) {
    if (
      event.button !== 0 ||
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey ||
      window.matchMedia("(prefers-reduced-motion: reduce)").matches
    )
      return;
    const effect = effectRef.current;
    timersRef.current.forEach(clearTimeout);
    timersRef.current.clear();
    effect
      .querySelectorAll(".gooey-particle")
      .forEach((particle) => particle.remove());
    for (let index = 0; index < 10; index++) {
      const noise = (amount) => amount / 2 - Math.random() * amount;
      const angle = ((360 + noise(8)) / 10) * index * (Math.PI / 180);
      const duration = 650 + noise(200);
      const particle = document.createElement("span");
      particle.className = "gooey-particle";
      particle.style.setProperty("--start-x", `${32 * Math.cos(angle)}px`);
      particle.style.setProperty("--start-y", `${20 * Math.sin(angle)}px`);
      particle.style.setProperty("--end-x", `${8 * Math.cos(angle)}px`);
      particle.style.setProperty("--end-y", `${8 * Math.sin(angle)}px`);
      particle.style.setProperty("--time", `${duration}ms`);
      particle.style.setProperty("--scale", `${1 + noise(0.2)}`);
      const point = document.createElement("span");
      point.className = "gooey-point";
      particle.appendChild(point);
      effect.appendChild(particle);
      const timer = setTimeout(() => {
        particle.remove();
        timersRef.current.delete(timer);
      }, duration);
      timersRef.current.add(timer);
    }
  }

  return (
    <nav
      className="main-nav gooey-nav-container"
      aria-label="Əsas menyu"
      ref={containerRef}
    >
      <svg
        className="gooey-filter-definition"
        aria-hidden="true"
        focusable="false"
      >
        <defs>
          <filter
            id={filterId}
            x="-100%"
            y="-100%"
            width="300%"
            height="300%"
            colorInterpolationFilters="sRGB"
          >
            <feGaussianBlur in="SourceGraphic" stdDeviation="3" result="blur" />
            <feColorMatrix
              in="blur"
              mode="matrix"
              values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 20 -8"
              result="goo"
            />
            <feComposite in="SourceGraphic" in2="goo" operator="atop" />
          </filter>
        </defs>
      </svg>
      <span
        className="gooey-effect"
        aria-hidden="true"
        ref={effectRef}
        style={{ filter: `url(#${filterId})` }}
      />
      {items.map((item) => (
        <a
          key={item.path}
          href={`#${item.path}`}
          className={isActive(path, item.path) ? "active" : ""}
          aria-current={isActive(path, item.path) ? "page" : undefined}
          onClick={burst}
        >
          <item.icon size={15} />
          {item.title}
        </a>
      ))}
      {children}
    </nav>
  );
}
