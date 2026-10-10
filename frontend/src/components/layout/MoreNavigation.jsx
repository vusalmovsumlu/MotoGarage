import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from "react";
import {
  Bell,
  CalendarDays,
  ChevronDown,
  ClipboardList,
  Fuel,
  ShoppingBag,
  Wallet,
  Wrench,
} from "lucide-react";
import BranchedMenu from "../ui/BranchedMenu.jsx";
import "./MoreNavigation.css";

const groups = [
  {
    label: "Planlar və alış-veriş",
    children: [
      {
        value: "/appointments",
        label: "Görüşlərim",
        icon: <CalendarDays size={16} />,
      },
      {
        value: "/orders",
        label: "Sifarişlərim",
        icon: <ClipboardList size={16} />,
      },
      { value: "/stores", label: "Mağazalar", icon: <ShoppingBag size={16} /> },
    ],
  },
  {
    label: "Nəqliyyat qeydləri",
    children: [
      { value: "/records/fuel", label: "Yanacaq", icon: <Fuel size={16} /> },
      {
        value: "/records/expenses",
        label: "Xərclər",
        icon: <Wallet size={16} />,
      },
      {
        value: "/records/service",
        label: "Servis tarixçəsi",
        icon: <Wrench size={16} />,
      },
      {
        value: "/records/reminders",
        label: "Xatırlatmalar",
        icon: <Bell size={16} />,
      },
    ],
  },
];

export default function MoreNavigation({ path }) {
  const detailsRef = useRef(null);
  const summaryRef = useRef(null);
  const panelRef = useRef(null);
  const animationRef = useRef(null);
  const [menuState, setMenuState] = useState({ path, open: false });
  const open = menuState.path === path && menuState.open;
  const items = groups;
  const recordKind = path.match(
    /^\/garage\/[^/]+\/(fuel|expenses|service|reminders)$/,
  )?.[1];
  const menuPath = recordKind ? `/records/${recordKind}` : path;
  const activeValue =
    items
      .flatMap((item) => item.children)
      .find(
        (item) =>
          menuPath === item.value || menuPath.startsWith(`${item.value}/`),
      )?.value ?? "";

  const close = useCallback(() => {
    if (panelRef.current?.contains(document.activeElement))
      summaryRef.current?.focus();
    setMenuState((current) =>
      current.open ? { ...current, open: false } : current,
    );
  }, []);
  useEffect(() => {
    if (!open) return undefined;
    const outside = (event) => {
      if (!detailsRef.current?.contains(event.target)) close();
    };
    document.addEventListener("pointerdown", outside);
    return () => document.removeEventListener("pointerdown", outside);
  }, [open, close]);

  useLayoutEffect(() => {
    const details = detailsRef.current;
    const panel = panelRef.current;
    if (!open && !details.open) return;
    if (open) details.open = true;

    // Sample the current frame before canceling so quick reversals remain smooth.
    const current = getComputedStyle(panel);
    const from = { opacity: current.opacity, transform: current.transform };
    const to = {
      opacity: open ? "1" : "0",
      transform: open
        ? "translateY(0) scale(1)"
        : "translateY(-8px) scale(0.98)",
    };
    animationRef.current?.cancel();
    const reduced = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    const animation = panel.animate([from, to], {
      duration: reduced ? 0 : open ? 240 : 180,
      easing: "cubic-bezier(0.23, 1, 0.32, 1)",
      fill: "both",
    });
    animationRef.current = animation;
    animation.onfinish = () => {
      if (animationRef.current !== animation) return;
      Object.assign(panel.style, to);
      if (!open) details.open = false;
      animation.cancel();
      animationRef.current = null;
    };
  }, [open]);

  useEffect(
    () => () => {
      if (animationRef.current) animationRef.current.onfinish = null;
      animationRef.current?.cancel();
      animationRef.current = null;
    },
    [],
  );

  useLayoutEffect(() => {
    if (!open) return undefined;
    const position = () => {
      const panel = panelRef.current;
      const anchor = summaryRef.current?.getBoundingClientRect();
      if (!panel || !anchor) return;
      const width = panel.offsetWidth;
      const left = Math.max(
        16,
        Math.min(anchor.left, window.innerWidth - width - 16),
      );
      const top = anchor.bottom + 12;
      Object.assign(panel.style, {
        left: `${left}px`,
        top: `${top}px`,
        maxHeight: `${Math.max(0, window.innerHeight - top - 16)}px`,
      });
    };
    position();
    const observer = new ResizeObserver(position);
    observer.observe(summaryRef.current);
    const header = detailsRef.current.closest("header");
    if (header) observer.observe(header);
    window.addEventListener("resize", position);
    window.addEventListener("scroll", position, true);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", position);
      window.removeEventListener("scroll", position, true);
    };
  }, [open]);

  return (
    <details
      className={`nav-more${activeValue ? " nav-more--active" : ""}`}
      ref={detailsRef}
      onKeyDown={(event) => {
        if (event.key === "Escape" && open) {
          event.stopPropagation();
          close();
          summaryRef.current.focus();
        }
      }}
    >
      <summary
        ref={summaryRef}
        aria-expanded={open}
        onClick={(event) => {
          event.preventDefault();
          setMenuState((current) => ({
            path,
            open: current.path === path ? !current.open : true,
          }));
        }}
      >
        Digər <ChevronDown className="nav-more-chevron" size={13} />
      </summary>
      <div
        className="more-menu-panel"
        ref={panelRef}
        data-state={open ? "open" : "closed"}
        aria-hidden={!open}
        inert={!open}
      >
        <BranchedMenu
          items={items}
          defaultOpen={items.map((_, index) => index)}
          activeValue={activeValue}
          onSelect={close}
          width={256}
          rowHeight={38}
          fontSize={12}
        />
      </div>
    </details>
  );
}
