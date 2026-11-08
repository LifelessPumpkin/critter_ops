# Animal form and variant selection UX report

## Files changed

Created:
- `apps/gilligan-web/src/components/animals/VariantPicker.tsx`
- `documentation/animal-form-ux-report.md`

Modified:
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.tsx`
- `apps/gilligan-web/src/components/animals/AnimalForm.tsx`
- `apps/gilligan-web/src/components/animals/AnimalOverview.tsx`
- `apps/gilligan-web/src/components/animals/AnimalReferenceFields.test.tsx`
- `apps/gilligan-web/src/components/animals/AnimalForm.test.tsx`
- `apps/gilligan-web/src/app/globals.css`
- `documentation/animal-variant-assignments.md`

## UI changes

Add and Edit use the same full-width searchable selector. Catalog values and custom values selected through “Add …” appear in one wrapping chip collection. The separate assignment heading and Custom Variant Type dropdown are removed. Selected catalog options are disabled and marked Selected. Custom duplicates use normalized case/whitespace comparisons within the stored type and show contextual feedback.

Compact × buttons have accessible removal names. Custom chip names are edit buttons that open a small text editor with Save/Cancel and Enter/Escape support. Saving preserves the original ID/type. Removal and completed editing return focus to search. Existing Ginger combobox, field, button, filter-chip and error-panel conventions are reused; no UI library was added.

Animal Group is labeled Animal Category in forms and list filters. Structured species populate and lock the category, with explanatory text. Custom species support manual category selection. A manually selected category can filter initial species searches; after selecting structured species, search allows choosing another species and derives its category.

The selector label follows PreferredVariantType. Missing, unknown or Other values fall back to Variant. New custom values inherit that preference, with Other as the generic fallback. The responsive form retains Name/Category, full-width Species and variant selection, then the existing detail fields. Below 600px, a scoped animal-form rule moves navigation into a horizontally scrollable row above the form so the existing sidebar does not squeeze the editor. Chips wrap and long names can break within the available width.

Blocking errors use the red banner above Animal Details. Missing/invalid fields also receive field-level errors, which clear as corrected. Incompatible selections are named in the banner and marked in the chip collection. Submission/API errors can be dismissed. Search failures, invalid custom length and duplicate feedback remain within the selector.

## API integration

Skipper already exposes Species.PreferredVariantType and the collection DTOs; no Skipper, Professor, migration or backend changes were needed. Existing collection payloads preserve structured IDs, optional assignment IDs, custom values and stored types. Existing server duplicate and compatibility checks remain unchanged. No calls to GBIF or hardcoded species classifications were added.

Howell Sync follow-up: future classification rules should populate the existing PreferredVariantType field. Full classification/import policy remains out of scope.

## Data preservation

Existing assignments initialize as chips. Historical custom types remain unchanged even when the species preference differs. Changing species never removes or converts an assignment; incompatible catalog selections require explicit removal or a compatible species. Custom values survive species changes.

Existing draft serialization/restoration continues to preserve assignments, IDs, types, category, species and other fields across enclosure creation. The returned enclosure is still selected. Editing a custom chip must be saved with Save value/Enter before navigating away; Cancel leaves the original value intact.

Animal details and list displays were reviewed and retain the existing complete detail list and compact list summary. They were not redesigned.

## Actual testing

| Check | Result | Details |
| --- | --- | --- |
| Gilligan production build | PASS | `npm run build`; compiled, TypeScript and static generation completed. Initial sandbox attempt could not download Google Fonts; retry with network access succeeded. |
| Gilligan tests | PASS | 40 tests across 5 files, zero failures. |
| TypeScript | PASS | `tsc --noEmit` and production build type checking. |
| Changed form component lint | PASS | ESLint on VariantPicker, AnimalReferenceFields and AnimalForm. |
| Skipper build | NOT RUN | Backend unchanged in this refinement. |
| Backend tests | NOT RUN | Backend unchanged in this refinement. |
| Manual desktop interactions | PASS | Verified dog category/Breed label, two breeds, custom entry, disabled duplicate option, preserved chips and blocking banner after changing to ball python, and explicit removal clearing the banner. No records were submitted. |
| Manual phone layout | PASS | Checked at 390 × 844; corrected sidebar squeeze with a form-scoped layout rule and verified custom entry without catalog connectivity. |
| Manual create/edit persistence and enclosure roundtrip | NOT RUN | Covered by automated tests; no manual database writes performed. |

Automated coverage includes preferred labels and fallback, category consistency, multiple catalog selections, custom/mixed selections, normalized duplicates, chip removal, custom edits preserving historical type/ID, species incompatibility, banner placement, field-error correction, API errors, keyboard selection/focus, and existing enclosure draft regressions.

## Known limitations

Manual end-to-end saved-record and enclosure-roundtrip scenarios remain outstanding. Desktop and phone visual/interaction checks were performed; the separate preview lacked backend connectivity, allowing offline-entry feedback to be checked. The standard form intentionally has no independent type selector; additional characteristics are deferred. No database rewrite, importer, genetics feature, push or pull request was performed.
