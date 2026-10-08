import { SearchBar } from "../../../components/ui/SearchBar.jsx";
import { roleNames } from "../../../shared/constants/labels.js";
import { Button } from "../../../components/ui/Button.jsx";

export function AdminUsersFilters({
  setFilters,
  search,
  role,
  setSearch,
  setRole,
}) {
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        setFilters({ search, role });
      }}
    >
      <SearchBar
        value={search}
        onChange={setSearch}
        placeholder="Ad və ya email axtarın…"
      >
        <select
          aria-label="Rol üzrə filter"
          value={role}
          onChange={(event) => setRole(event.target.value)}
        >
          <option value="">Bütün rollar</option>
          {Object.entries(roleNames).map(([value, title]) => (
            <option value={value} key={value}>
              {title}
            </option>
          ))}
        </select>
        <Button type="submit">Axtar</Button>
      </SearchBar>
    </form>
  );
}
