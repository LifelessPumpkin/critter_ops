import type { Animal } from "@/lib/api/animals";

type AnimalCardProps = {
  animal: Animal;
};

const dateFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
});

function formatDate(date: string) {
  return dateFormatter.format(new Date(`${date}T00:00:00`));
}

export function AnimalCard({ animal }: AnimalCardProps) {
  return (
    <article className="dashboard-card animal-card">
      <div>
        <div className="enclosure-card-header">
          <div>
            <p className="eyebrow-text">{animal.animalType}</p>
            <h2 className="card-title">{animal.name}</h2>
          </div>
          <span className="status-pill">{animal.status}</span>
        </div>

        <dl className="enclosure-details">
          <div>
            <dt>Species</dt>
            <dd>{animal.species}</dd>
          </div>

          {animal.subspeciesOrMorph ? (
            <div>
              <dt>Morph</dt>
              <dd>{animal.subspeciesOrMorph}</dd>
            </div>
          ) : null}

          <div>
            <dt>Sex</dt>
            <dd>{animal.sex}</dd>
          </div>

          <div>
            <dt>Enclosure</dt>
            <dd>#{animal.enclosureId}</dd>
          </div>

          <div>
            <dt>Acquired</dt>
            <dd>{formatDate(animal.acquiredDate)}</dd>
          </div>
        </dl>
      </div>
    </article>
  );
}
