import { SearchBar } from "../../ui/SearchBar.jsx";
import { Button } from "../../ui/Button.jsx";

export function CatalogFilters({
  products,
  min,
  max,
  setError,
  setFilters,
  search,
  extra,
  setSearch,
  setExtra,
  setMin,
  setMax,
}) {
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        if (
          products &&
          (Number(min) < 0 ||
            Number(max) < 0 ||
            (min !== "" && max !== "" && Number(min) > Number(max)))
        ) {
          setError("Qiymət aralığını düzgün daxil edin.");
          return;
        }
        setError("");
        setFilters({
          search,
          [products ? "brand" : "address"]: extra,
          ...(products ? { minPrice: min, maxPrice: max } : {}),
        });
      }}
    >
      <SearchBar
        value={search}
        onChange={setSearch}
        placeholder={products ? "Məhsul axtarın…" : "Ad və ya xidmət axtarın…"}
      >
        <input
          aria-label={products ? "Marka" : "Ünvan"}
          placeholder={products ? "Marka" : "Ünvan / şəhər"}
          value={extra}
          onChange={(event) => setExtra(event.target.value)}
        />
        {products && (
          <>
            <input
              className="price-filter"
              aria-label="Minimum qiymət"
              placeholder="Min ₼"
              type="number"
              min="0"
              step="0.01"
              value={min}
              onChange={(event) => setMin(event.target.value)}
            />
            <input
              className="price-filter"
              aria-label="Maksimum qiymət"
              placeholder="Max ₼"
              type="number"
              min="0"
              step="0.01"
              value={max}
              onChange={(event) => setMax(event.target.value)}
            />
          </>
        )}
        <Button type="submit">Axtar</Button>
      </SearchBar>
    </form>
  );
}
