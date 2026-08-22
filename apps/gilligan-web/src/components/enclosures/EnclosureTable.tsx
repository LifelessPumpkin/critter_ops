import type { Enclosure } from "@/lib/api/enclosures";

type EnclosureTableProps = {
  enclosures: Enclosure[];
};

export function EnclosureTable({ enclosures }: EnclosureTableProps) {
  return (
    <div className="data-table-scroll">
      <table className="data-table enclosure-table">
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Type</th>
            <th scope="col">Status</th>
            <th scope="col">Size</th>
            <th scope="col">Capacity</th>
            <th scope="col">Material</th>
          </tr>
        </thead>
        <tbody>
          {enclosures.map((enclosure) => (
            <tr key={enclosure.id}>
              <td>
                <span className="table-primary-text">{enclosure.name}</span>
              </td>
              <td>{formatEnumLabel(enclosure.type)}</td>
              <td>
                <span className="status-pill status-pill-table">{formatEnumLabel(enclosure.status)}</span>
              </td>
              <td>{enclosure.sizeLabel || "-"}</td>
              <td>{formatCapacity(enclosure.maxAnimalCapacity)}</td>
              <td>{enclosure.material || "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatCapacity(capacity: number | null | undefined) {
  return capacity === null || capacity === undefined ? "-" : capacity;
}

function formatEnumLabel(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
