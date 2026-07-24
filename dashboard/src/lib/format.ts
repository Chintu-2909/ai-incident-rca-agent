export function formatTechnicalLabel(
  value?: string | null,
): string {
  if (!value) {
    return "Not available";
  }

  return value
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/([A-Z])([A-Z][a-z])/g, "$1 $2")
    .trim();
}
