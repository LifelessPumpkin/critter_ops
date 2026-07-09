import type { Enclosure } from "@/lib/api/enclosures";

type EnclosureCardProps = {
  enclosure: Enclosure;
};

export function EnclosureCard({ enclosure }: EnclosureCardProps) {
  return (
    <article className="dashboard-card enclosure-card">
      <div>
        <div className="enclosure-card-header">
          <div>
            <p className="eyebrow-text">{enclosure.type}</p>
            <h2 className="card-title">{enclosure.name}</h2>
          </div>
          <span className="status-pill">{enclosure.status}</span>
        </div>

        <dl className="enclosure-details">
          <div>
            <dt>Location</dt>
            <dd>{enclosure.location}</dd>
          </div>

          {enclosure.sizeLabel ? (
            <div>
              <dt>Size</dt>
              <dd>{enclosure.sizeLabel}</dd>
            </div>
          ) : null}

          {enclosure.material ? (
            <div>
              <dt>Material</dt>
              <dd>{enclosure.material}</dd>
            </div>
          ) : null}

          {enclosure.maxAnimalCapacity !== null && enclosure.maxAnimalCapacity !== undefined ? (
            <div>
              <dt>Capacity</dt>
              <dd>{enclosure.maxAnimalCapacity}</dd>
            </div>
          ) : null}
        </dl>
      </div>
    </article>
  );
}
