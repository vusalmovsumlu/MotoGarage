// Adapted from React Bits BranchedMenu. License: /licenses/react-bits.txt
"use client";

import {
  isValidElement,
  useId,
  useLayoutEffect,
  useRef,
  useState,
} from "react";
import { HugeiconsIcon } from "@hugeicons/react";
import { ChevronDown } from "lucide-react";
import "./BranchedMenu.css";

const PAD = 6;
const MARK = 16;

const renderIcon = (icon) =>
  isValidElement(icon) ? (
    icon
  ) : (
    <HugeiconsIcon icon={icon} size={16} strokeWidth={1.8} />
  );
const toSet = (open) =>
  new Set(Array.isArray(open) ? open : open >= 0 ? [open] : []);

export default function BranchedMenu({
  items = [],
  defaultOpen = 0,
  activeValue = "",
  onSelect,
  onToggle,
  color = "var(--text)",
  accentColor = "var(--primary)",
  lineColor = "var(--line)",
  width = 240,
  rowHeight = 36,
  indent = 40,
  trunk = 14,
  radius = 10,
  lineWidth = 1.5,
  fontSize = 14,
  drawDuration = 400,
  foldDuration = 300,
  className = "",
}) {
  const [open, setOpen] = useState(() => toSet(defaultOpen));
  const active = activeValue;
  const menuId = useId();
  const navRef = useRef(null);
  const heads = useRef([]);
  const markerRef = useRef(null);

  const activeSection = items.findIndex((it) =>
    it.children?.some((kid) => kid.value === active),
  );
  const markerShown = activeSection >= 0 && open.has(activeSection);
  useLayoutEffect(() => {
    const place = (glide) => {
      const m = markerRef.current;
      const el = heads.current[activeSection];
      if (!m) return;
      const on = markerShown && el;
      if (!glide) m.style.transition = "none";
      if (on) m.style.top = `${el.offsetTop + (el.offsetHeight - MARK) / 2}px`;
      m.toggleAttribute("data-on", Boolean(on));
      if (!glide) {
        void m.offsetHeight;
        m.style.transition = "";
      }
    };
    place(true);
    const ro = new ResizeObserver(() => {
      place(false);
    });
    if (navRef.current) ro.observe(navRef.current);
    return () => ro.disconnect();
  }, [activeSection, markerShown, items, fontSize, rowHeight]);

  const select = (event, value, item) => {
    if (
      event.button === 0 &&
      !event.metaKey &&
      !event.ctrlKey &&
      !event.shiftKey &&
      !event.altKey
    )
      onSelect?.(value, item);
  };
  const toggle = (i) => {
    const next = new Set(open);
    const isOpen = !next.has(i);
    if (isOpen) next.add(i);
    else next.delete(i);
    setOpen(next);
    onToggle?.(i, isOpen);
  };

  const r = Math.min(radius, rowHeight / 2 - 2);
  const endX = indent - 8;
  const rowY = (k) => PAD + k * rowHeight + rowHeight / 2;
  const branch = (k) =>
    `M ${trunk} ${rowY(k) - r} A ${r} ${r} 0 0 0 ${trunk + r} ${rowY(k)} H ${endX}`;
  const reach = (k) =>
    `M ${trunk} 0 V ${rowY(k) - r} A ${r} ${r} 0 0 0 ${trunk + r} ${rowY(k)} H ${endX}`;
  const length = (k) => rowY(k) - r + (Math.PI * r) / 2 + (endX - trunk - r);

  return (
    <nav
      ref={navRef}
      aria-label="Əlavə keçidlər"
      className={`branched-menu${className ? ` ${className}` : ""}`}
      style={{
        "--bm-w": `${width}px`,
        "--bm-ink": color,
        "--bm-accent": accentColor,
        "--bm-line": lineColor,
        "--bm-font": `${fontSize}px`,
        "--bm-row": `${rowHeight}px`,
        "--bm-indent": `${indent}px`,
        "--bm-line-w": lineWidth,
        "--bm-draw": `${drawDuration}ms`,
        "--bm-fold": `${foldDuration}ms`,
      }}
    >
      <span
        ref={markerRef}
        className="branched-menu__marker"
        aria-hidden="true"
      />
      {items.map((item, i) => {
        const kids = item.children;
        const isOpen = kids ? open.has(i) : false;
        const leafValue = item.value ?? item.label;
        const leafActive = !kids && leafValue === active;
        const Head = kids ? "button" : "a";
        const bodyH = kids ? PAD * 2 + kids.length * rowHeight : 0;
        return (
          <div
            key={item.value ?? item.label}
            className="branched-menu__section"
            data-open={isOpen ? "" : undefined}
          >
            <Head
              ref={(el) => {
                heads.current[i] = el;
              }}
              type={kids ? "button" : undefined}
              href={kids ? undefined : `#${leafValue}`}
              className="branched-menu__head"
              aria-expanded={kids ? isOpen : undefined}
              aria-controls={kids ? `${menuId}-${i}` : undefined}
              aria-current={leafActive ? "page" : undefined}
              data-active={leafActive ? "" : undefined}
              onClick={(event) =>
                kids ? toggle(i) : select(event, leafValue, item)
              }
            >
              {item.label}
              {kids ? (
                <ChevronDown
                  size={13}
                  className="branched-menu__toggle"
                  aria-hidden="true"
                />
              ) : null}
            </Head>
            {kids ? (
              <div
                className="branched-menu__body"
                id={`${menuId}-${i}`}
                aria-hidden={!isOpen}
                inert={!isOpen}
              >
                <div className="branched-menu__fold">
                  <div
                    className="branched-menu__tree"
                    style={{ height: bodyH }}
                  >
                    <svg
                      className="branched-menu__lines"
                      width={indent}
                      height={bodyH}
                      aria-hidden="true"
                    >
                      <path
                        className="branched-menu__base"
                        d={`M ${trunk} 0 V ${rowY(kids.length - 1) - r}`}
                      />
                      {kids.map((kid, k) => (
                        <path
                          key={kid.value}
                          className="branched-menu__base"
                          d={branch(k)}
                        />
                      ))}
                      {kids.map((kid, k) => (
                        <path
                          key={kid.value}
                          className="branched-menu__reach"
                          d={reach(k)}
                          style={{
                            strokeDasharray: length(k),
                            strokeDashoffset:
                              kid.value === active ? 0 : length(k),
                          }}
                        />
                      ))}
                    </svg>
                    {kids.map((kid) => (
                      <a
                        key={kid.value}
                        href={`#${kid.value}`}
                        className="branched-menu__item"
                        aria-current={kid.value === active ? "page" : undefined}
                        data-active={kid.value === active ? "" : undefined}
                        tabIndex={isOpen ? 0 : -1}
                        onClick={(event) => select(event, kid.value, kid)}
                      >
                        {kid.icon ? (
                          <span
                            className="branched-menu__icon"
                            aria-hidden="true"
                          >
                            {renderIcon(kid.icon)}
                          </span>
                        ) : null}
                        <span className="branched-menu__label">
                          {kid.label}
                        </span>
                      </a>
                    ))}
                  </div>
                </div>
              </div>
            ) : null}
          </div>
        );
      })}
    </nav>
  );
}
