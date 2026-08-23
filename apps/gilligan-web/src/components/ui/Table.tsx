import type { ReactNode } from "react";

export type TableColumn<Row> = {
  header: string;
  render: (row: Row) => ReactNode;
};

type TableProps<Row> = {
  ariaLabel?: string;
  columns: TableColumn<Row>[];
  emptyMessage?: string;
  getRowKey: (row: Row) => string | number;
  rows: Row[];
  tableClassName?: string;
};

export function Table<Row>({
  ariaLabel,
  columns,
  emptyMessage = "No records found.",
  getRowKey,
  rows,
  tableClassName,
}: TableProps<Row>) {
  return (
    <div className="data-table-scroll">
      <table className={["data-table", tableClassName].filter(Boolean).join(" ")} aria-label={ariaLabel}>
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.header} scope="col">
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length > 0 ? (
            rows.map((row) => (
              <tr key={getRowKey(row)}>
                {columns.map((column) => (
                  <td key={column.header}>{column.render(row)}</td>
                ))}
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={columns.length} className="data-table-empty">
                {emptyMessage}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
