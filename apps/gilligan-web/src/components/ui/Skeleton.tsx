import type { CSSProperties, HTMLAttributes } from "react";

type SkeletonDimension = CSSProperties["width"];

type SkeletonProps = HTMLAttributes<HTMLElement> & {
  height?: CSSProperties["height"];
  inline?: boolean;
  width?: SkeletonDimension;
};

export function Skeleton({ className, height, inline = false, style, width, ...props }: SkeletonProps) {
  const Component = inline ? "span" : "div";

  return (
    <Component
      aria-hidden="true"
      className={["skeleton", inline ? "skeleton-inline" : null, className].filter(Boolean).join(" ")}
      style={{ ...style, height, width }}
      {...props}
    />
  );
}

type SkeletonTextProps = Omit<SkeletonProps, "height" | "inline"> & {
  variant?: "heading" | "body" | "label" | "metadata";
};

export function SkeletonText({ className, variant = "body", width = "100%", ...props }: SkeletonTextProps) {
  return (
    <Skeleton
      className={["skeleton-text", `skeleton-text-${variant}`, className].filter(Boolean).join(" ")}
      width={width}
      {...props}
    />
  );
}

type SkeletonButtonProps = Omit<SkeletonProps, "height" | "inline"> & {
  size?: "small" | "medium";
};

export function SkeletonButton({ className, size = "medium", width = "7rem", ...props }: SkeletonButtonProps) {
  return (
    <Skeleton
      className={["skeleton-button", `skeleton-button-${size}`, className].filter(Boolean).join(" ")}
      width={width}
      {...props}
    />
  );
}

type SkeletonInputProps = Omit<SkeletonProps, "height" | "inline"> & {
  multiline?: boolean;
};

export function SkeletonInput({ className, multiline = false, width = "100%", ...props }: SkeletonInputProps) {
  return (
    <Skeleton
      className={["skeleton-input", multiline ? "skeleton-input-multiline" : null, className]
        .filter(Boolean)
        .join(" ")}
      width={width}
      {...props}
    />
  );
}

export function SkeletonBadge({ className, width = "4.5rem", ...props }: Omit<SkeletonProps, "height" | "inline">) {
  return <Skeleton className={["skeleton-badge", className].filter(Boolean).join(" ")} width={width} {...props} />;
}

export type SkeletonTableColumn = {
  header: string;
  width?: SkeletonDimension;
};

type SkeletonTableRowProps = {
  columns: SkeletonTableColumn[];
  rowIndex?: number;
};

export function SkeletonTableRow({ columns, rowIndex = 0 }: SkeletonTableRowProps) {
  return (
    <tr className="skeleton-table-row" aria-hidden="true">
      {columns.map((column, columnIndex) => (
        <td key={`${column.header}-${columnIndex}`}>
          <SkeletonText
            variant="metadata"
            width={getStableCellWidth(column.width, rowIndex, columnIndex)}
          />
        </td>
      ))}
    </tr>
  );
}

type SkeletonTableProps = {
  ariaLabel?: string;
  columns: SkeletonTableColumn[];
  rows?: number;
  tableClassName?: string;
};

export function SkeletonTable({ ariaLabel = "Loading records", columns, rows = 6, tableClassName }: SkeletonTableProps) {
  return (
    <div className="data-table-scroll" role="status" aria-label={ariaLabel}>
      <table className={["data-table", "skeleton-table", tableClassName].filter(Boolean).join(" ")}>
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.header} scope="col">{column.header}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {Array.from({ length: rows }, (_, rowIndex) => (
            <SkeletonTableRow columns={columns} key={rowIndex} rowIndex={rowIndex} />
          ))}
        </tbody>
      </table>
      <span className="visually-hidden">Loading…</span>
    </div>
  );
}

type SkeletonPageHeaderProps = {
  action?: boolean;
  badgeWidth?: SkeletonDimension;
  subtitleWidth?: SkeletonDimension;
  titleWidth?: SkeletonDimension;
};

export function SkeletonPageHeader({
  action = false,
  badgeWidth = "5rem",
  subtitleWidth = "24rem",
  titleWidth = "14rem",
}: SkeletonPageHeaderProps) {
  return (
    <section
      className={["page-heading", action ? "page-heading-with-action" : null].filter(Boolean).join(" ")}
      role="status"
      aria-label="Loading page"
    >
      <div className="skeleton-page-heading-copy">
        <SkeletonBadge className="skeleton-page-eyebrow" width={badgeWidth} />
        <SkeletonText className="skeleton-page-title" variant="heading" width={titleWidth} />
        <SkeletonText variant="body" width={subtitleWidth} />
      </div>
      {action ? <SkeletonButton /> : null}
      <span className="visually-hidden">Loading page…</span>
    </section>
  );
}

function getStableCellWidth(width: SkeletonDimension | undefined, rowIndex: number, columnIndex: number) {
  if (width !== undefined) {
    return width;
  }

  const widths = ["72%", "58%", "84%", "66%"];
  return widths[(rowIndex + columnIndex) % widths.length];
}
