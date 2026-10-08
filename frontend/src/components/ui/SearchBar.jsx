import { Search } from "lucide-react";

export function SearchBar({
  value,
  onChange,
  placeholder = "Axtar…",
  children,
}) {
  return (
    <div className="filter-bar">
      <div className="search-field">
        <Search size={17} />
        <input
          aria-label={placeholder}
          placeholder={placeholder}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      </div>
      {children}
    </div>
  );
}
