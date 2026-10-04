import type { AnimalListItem } from "@/lib/api/animals";
import Link from "next/link";
import { Badge, SkeletonTableRow, Table, type SkeletonTableColumn, type TableColumn, getStatusBadgeVariant } from "@/components/ui";

type AnimalTableProps = {
  animals: AnimalListItem[];
  ariaLabel: string;
  loadingMore?: boolean;
};

export function AnimalTable({ animals, ariaLabel, loadingMore = false }: AnimalTableProps) {
  return (
    <Table
      ariaLabel={ariaLabel}
      columns={columns}
      getRowKey={(animal) => animal.id}
      rows={animals}
      tableClassName="animal-table"
      trailingRows={loadingMore ? Array.from({ length: 3 }, (_, index) => (
        <SkeletonTableRow columns={skeletonColumns} key={`loading-${index}`} rowIndex={index} />
      )) : undefined}
    />
  );
}

const columns: TableColumn<AnimalListItem>[] = [
  {
    header: "Animal",
    render: (animal) => (
      <div className="animal-identity">
        <span className="animal-avatar" aria-hidden="true">{getInitials(animal.name)}</span>
        <span>
          <Link className="table-primary-link" href={`/animals/${animal.id}/activity`}>{animal.name}</Link>
          <span className="animal-cell-metadata">#{animal.id}</span>
        </span>
      </div>
    ),
  },
  {
    header: "Sex",
    render: (animal) => formatEnumLabel(animal.sex),
  },
  {
    header: "Species",
    render: (animal) => animal.species,
  },
  {
    header: "Age / DOB",
    render: (animal) => animal.birthDate ? (
      <span>
        <span className="animal-age">{formatAge(animal.birthDate)}</span>
        <span className="animal-cell-metadata">{animal.birthDateIsEstimated ? "Est. " : ""}{formatDate(animal.birthDate)}</span>
      </span>
    ) : <span className="animal-empty-value">—</span>,
  },
  {
    header: "Morph / Breed",
    render: (animal) => animal.subspeciesOrMorph?.trim() || <span className="animal-empty-value">—</span>,
  },
  {
    header: "Status",
    render: (animal) => <Badge variant={getStatusBadgeVariant(animal.status)}>{formatEnumLabel(animal.status)}</Badge>,
  },
  {
    header: "Enclosure",
    render: (animal) => animal.enclosureName ? (
      <span>
        <Link className="table-primary-link" href={`/enclosures/${animal.enclosureId}/activity`}>{animal.enclosureName}</Link>
        {animal.enclosureLocation ? <span className="animal-cell-metadata">{animal.enclosureLocation}</span> : null}
      </span>
    ) : <span className="animal-empty-value">Unassigned</span>,
  },
];

const skeletonColumns: SkeletonTableColumn[] = columns.map((column) => ({ header: column.header }));

const dateFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
});

function formatDate(date: string) {
  return dateFormatter.format(new Date(`${date}T00:00:00`));
}

function formatAge(birthDate: string) {
  const birth = new Date(`${birthDate}T00:00:00`);
  const today = new Date();
  let months = (today.getFullYear() - birth.getFullYear()) * 12 + today.getMonth() - birth.getMonth();
  if (today.getDate() < birth.getDate()) months -= 1;
  if (months < 1) return "< 1m";
  const years = Math.floor(months / 12);
  const remainingMonths = months % 12;
  if (years === 0) return `${remainingMonths}m`;
  return remainingMonths ? `${years}y ${remainingMonths}m` : `${years}y`;
}

function getInitials(name: string) {
  return name.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join("").toUpperCase() || "?";
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
