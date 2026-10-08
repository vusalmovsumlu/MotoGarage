export function Preview({ items, render, empty }) {
  return items?.length ? (
    <div className="preview-list">{items.map(render)}</div>
  ) : (
    <p className="quiet-empty">{empty}</p>
  );
}
