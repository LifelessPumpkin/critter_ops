const prefix = "critterops:add-animal:";
const maxAge = 6 * 60 * 60 * 1000;
export function validWorkflow(value?: string): value is string { return Boolean(value && /^[0-9a-f-]{36}$/i.test(value)); }
export function saveAnimalDraft(workflow: string, form: object) {
  if (!validWorkflow(workflow)) throw new Error("Invalid animal workflow.");
  sessionStorage.setItem(prefix + workflow, JSON.stringify({ version: 1, savedAt: Date.now(), form }));
}
export function loadAnimalDraft<T extends object>(workflow: string | undefined, defaults: T): T | null {
  if (!validWorkflow(workflow)) return null;
  try {
    const raw = sessionStorage.getItem(prefix + workflow);
    if (!raw) return null;
    const draft = JSON.parse(raw);
    if (draft.version !== 1 || !Number.isFinite(draft.savedAt) || Date.now() - draft.savedAt > maxAge || draft.savedAt > Date.now() || !draft.form || typeof draft.form !== "object") {
      clearAnimalDraft(workflow); return null;
    }
    // Only fields belonging to this form are restored; malformed primitive values are ignored.
    const result = { ...defaults };
    for (const key of Object.keys(defaults) as (keyof T)[]) {
      const value = draft.form[key];
      if (value === null && defaults[key] === null || typeof value === typeof defaults[key]) result[key] = value;
    }
    return result;
  } catch { return null; }
}
export function clearAnimalDraft(workflow?: string) {
  if (!validWorkflow(workflow)) return;
  try { sessionStorage.removeItem(prefix + workflow); } catch { /* Storage may be disabled. */ }
}
export function animalReturnUrl(workflow: string, enclosureId?: number) {
  return `/animals/new?workflow=${encodeURIComponent(workflow)}${enclosureId ? `&enclosureId=${enclosureId}` : ""}`;
}
