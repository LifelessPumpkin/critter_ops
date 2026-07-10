import type { Animal } from "@/lib/api/animals";

type AnimalCardProps = {
  animal: Animal;
  variant?: "active" | "deceased";
};

const dateFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
});

function formatDate(date: string) {
  return dateFormatter.format(new Date(`${date}T00:00:00`));
}

export function AnimalCard({ animal, variant = "active" }: AnimalCardProps) {
  const isDeceased = variant === "deceased";

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
            <dt>{isDeceased ? "Former Enclosure" : "Enclosure"}</dt>
            <dd>{animal.enclosureName}</dd>
          </div>

          {isDeceased ? (
            <>
              {animal.birthDate ? (
                <div>
                  <dt>Birth Date</dt>
                  <dd>{formatDate(animal.birthDate)}</dd>
                </div>
              ) : null}

              {animal.dispositionDate ? (
                <div>
                  <dt>Disposition Date</dt>
                  <dd>{formatDate(animal.dispositionDate)}</dd>
                </div>
              ) : null}

              {animal.dispositionReason ? (
                <div>
                  <dt>Disposition Reason</dt>
                  <dd>{animal.dispositionReason}</dd>
                </div>
              ) : null}
            </>
          ) : (
            <div>
              <dt>Acquired</dt>
              <dd>{formatDate(animal.acquiredDate)}</dd>
            </div>
          )}
        </dl>
      </div>
    </article>
  );
}
