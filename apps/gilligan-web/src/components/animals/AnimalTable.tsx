import type { Animal } from "@/lib/api/animals";
import Link from "next/link";
import { Badge, Table, type TableColumn, getStatusBadgeVariant } from "@/components/ui";

type AnimalTableProps = {
  animals: Animal[];
  ariaLabel: string;
};

export function AnimalTable({ animals, ariaLabel }: AnimalTableProps) {
  return <Table ariaLabel={ariaLabel} columns={columns} getRowKey={(animal) => animal.id} rows={animals} tableClassName="animal-table" />;
}

const columns: TableColumn<Animal>[] = [
  {
    header: "Name",
    render: (animal) => <Link className="table-primary-link" href={`/animals/${animal.id}/activity`}>{animal.name}</Link>,
  },
  {
    header: "Type",
    render: (animal) => formatEnumLabel(animal.animalType),
  },
  {
    header: "Species",
    render: (animal) => animal.species,
  },
  {
    header: "Status",
    render: (animal) => <Badge variant={getStatusBadgeVariant(animal.status)}>{formatEnumLabel(animal.status)}</Badge>,
  },
  {
    header: "Sex",
    render: (animal) => animal.sex,
  },
  {
    header: "Enclosure",
    render: (animal) => animal.enclosureName,
  },
  {
    header: "Acquired",
    render: (animal) => formatDate(animal.acquiredDate),
  },
];

const dateFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
});

function formatDate(date: string) {
  return dateFormatter.format(new Date(`${date}T00:00:00`));
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
