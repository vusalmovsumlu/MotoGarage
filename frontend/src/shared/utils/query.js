export function query(values) {
  const result = new URLSearchParams(
    Object.entries(values).filter(
      ([, value]) => value !== "" && value !== null && value !== undefined,
    ),
  ).toString();
  return result ? `?${result}` : "";
}
