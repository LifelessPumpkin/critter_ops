import type { ActivityRecord } from "@/lib/api/activity";

export function toDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function fromDateInputValue(value: string) {
  const [year, month, day] = value.split("-").map(Number);
  return new Date(year, month - 1, day);
}

export function addDays(value: string, days: number) {
  const date = fromDateInputValue(value);
  date.setDate(date.getDate() + days);
  return toDateInputValue(date);
}

export function startOfWeek(value: string) {
  const date = fromDateInputValue(value);
  const day = date.getDay();
  const offset = day === 0 ? -6 : 1 - day;
  date.setDate(date.getDate() + offset);
  return toDateInputValue(date);
}

export function getWeekDates(value: string) {
  const start = startOfWeek(value);
  return Array.from({ length: 7 }, (_, index) => addDays(start, index));
}

export function dateBoundaryToIso(date: string, endOfDay: boolean) {
  const boundary = new Date(`${date}T${endOfDay ? "23:59:59.999" : "00:00:00.000"}`);
  return boundary.toISOString();
}

export function localDateKey(value: string | Date) {
  const date = typeof value === "string" ? new Date(value) : value;
  return toDateInputValue(date);
}

export function formatAssociationNames(
  associations: ActivityRecord["animals"] | ActivityRecord["enclosures"],
) {
  return associations.length ? associations.map((association) => association.name).join(", ") : "—";
}

export function clampDateWindow(
  windowFrom: string | undefined,
  windowTo: string | undefined,
  filterFrom: string,
  filterTo: string,
) {
  const from = [windowFrom, filterFrom || undefined].filter(Boolean).sort().at(-1);
  const to = [windowTo, filterTo || undefined].filter(Boolean).sort().at(0);
  return { from, to };
}

export function daysBetween(from: string, to: string) {
  const milliseconds = fromDateInputValue(to).getTime() - fromDateInputValue(from).getTime();
  return Math.max(1, Math.round(milliseconds / 86_400_000) + 1);
}
