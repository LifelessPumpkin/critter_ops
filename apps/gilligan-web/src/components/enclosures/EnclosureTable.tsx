import type { Enclosure } from "@/lib/api/enclosures";
import { Badge, Table, type TableColumn, getStatusBadgeVariant } from "@/components/ui";

type EnclosureTableProps = {
  enclosures: Enclosure[];
};

export function EnclosureTable({ enclosures }: EnclosureTableProps) {
  return (
    <Table
      ariaLabel="Enclosures"
      columns={columns}
      getRowKey={(enclosure) => enclosure.id}
      rows={enclosures}
      tableClassName="enclosure-table"
    />
  );
}

const columns: TableColumn<Enclosure>[] = [
  {
    header: "Name",
    render: (enclosure) => <span className="table-primary-text">{enclosure.name}</span>,
  },
  {
    header: "Type",
    render: (enclosure) => formatEnumLabel(enclosure.type),
  },
  {
    header: "Status",
    render: (enclosure) => (
      <Badge variant={getStatusBadgeVariant(enclosure.status)}>{formatEnumLabel(enclosure.status)}</Badge>
    ),
  },
  {
    header: "Size",
    render: (enclosure) => enclosure.sizeLabel || "-",
  },
  {
    header: "Capacity",
    render: (enclosure) => formatCapacity(enclosure.maxAnimalCapacity),
  },
  {
    header: "Material",
    render: (enclosure) => enclosure.material || "-",
  },
];

function formatCapacity(capacity: number | null | undefined) {
  return capacity === null || capacity === undefined ? "-" : capacity;
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
