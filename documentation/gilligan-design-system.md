# Gilligan Design System

Gilligan uses a restrained operational interface for CritterOps. The design system is intentionally calm, readable, and efficient rather than decorative.

## Design Principles

- Clean, professional, and functional.
- Favor readable tables for operational lists.
- Use cards for dashboard summaries and compact information panels.
- Keep spacing generous enough for scanning without making pages feel sparse.
- Avoid gradients, glass effects, heavy shadows, oversized type, and decorative UI.

## Tokens

Tokens are defined in `apps/gilligan-web/src/app/globals.css`.

### Typography

- `--font-size-display`: display-level page moments.
- `--font-size-heading-1`: page titles.
- `--font-size-heading-2`: card and table-adjacent section headings.
- `--font-size-heading-3`: compact section headings.
- `--font-size-body`: default body text.
- `--font-size-small`: supporting UI text.
- `--font-size-caption`: badges, labels, and metadata.

### Color

- `--color-primary`: primary actions and active navigation.
- `--color-secondary`: secondary text and quiet controls.
- `--color-success`: positive or available statuses.
- `--color-warning`: needs-attention statuses.
- `--color-danger`: destructive actions and errors.
- `--color-info`: informational states.
- `--color-background`: app background.
- `--color-surface`: cards, forms, and top-level surfaces.
- `--color-border`: standard separators.
- `--color-text-primary`, `--color-text-secondary`, `--color-text-muted`: text hierarchy.

### Spacing

- `--space-1`: 4px
- `--space-2`: 8px
- `--space-3`: 12px
- `--space-4`: 16px
- `--space-6`: 24px
- `--space-8`: 32px
- `--space-12`: 48px
- `--space-16`: 64px

### Radius

- `--radius-small`: compact controls and icon marks.
- `--radius-medium`: default buttons, inputs, cards, and sections.
- `--radius-large`: larger framed surfaces when needed.

## Components

Reusable components live in `apps/gilligan-web/src/components/ui`.

- `Button` and `ButtonLink`: primary, secondary, danger, and ghost actions.
- `Input`: labeled text, search, and number inputs with validation display.
- `Select`: labeled dropdown with validation display.
- `Textarea`: multiline input with validation display.
- `Table`: standard operational table with headers, hover state, empty state, and responsive scroll container.
- `Card` and `SummaryCard`: summary and information panel surfaces.
- `Badge`: success, warning, error, info, and neutral status indicators.

Import from the barrel where practical:

```tsx
import { Badge, Button, Input, Table } from "@/components/ui";
```

## Page Structure

Pages should follow this hierarchy:

1. Breadcrumbs from the shared application shell.
2. Page heading with title and optional description.
3. Optional primary page action aligned with the heading.
4. Main content using shared UI components.

Operational collections should default to tables. Cards remain appropriate for dashboards, metrics, and short summary panels.
