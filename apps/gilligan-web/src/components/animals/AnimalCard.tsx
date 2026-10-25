import type { Animal } from "@/lib/api/animals";

type AnimalCardProps = {
  animal: Animal;
  variant?: "active" | "deceased" | "archived";
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
  const isArchived = variant === "archived";

  return (
    <article className="dashboard-card animal-card">
      <div>
        <div className="enclosure-card-header">
          <div>
            <p className="eyebrow-text">{animal.animalGroupName ?? animal.animalType}</p>
            <h2 className="card-title">{animal.name}</h2>
          </div>
          <span className="status-pill">{animal.status}</span>
        </div>

        <dl className="enclosure-details">
          <div>
            <dt>Species</dt>
            <dd>{animal.species}</dd>
          </div>

          {animal.speciesScientificName ? <div><dt>Scientific name</dt><dd>{animal.speciesScientificName}</dd></div> : null}
          {animal.speciesId && animal.originalSpeciesText && animal.originalSpeciesText !== animal.species ? <div><dt>Original species description</dt><dd>{animal.originalSpeciesText}</dd></div> : null}
          {animal.animalVariantId && animal.originalVariantText && animal.originalVariantText !== animal.subspeciesOrMorph ? <div><dt>Original variant description</dt><dd>{animal.originalVariantText}</dd></div> : null}

          {animal.subspeciesOrMorph ? (
            <div>
              <dt>{animal.variantType ?? "Variant"}</dt>
              <dd>{animal.subspeciesOrMorph}</dd>
            </div>
          ) : null}

          <div>
            <dt>Sex</dt>
            <dd>{animal.sex}</dd>
          </div>

          <div>
            <dt>{isDeceased || isArchived ? "Former Enclosure" : "Enclosure"}</dt>
            <dd>{animal.enclosureName}</dd>
          </div>

          {isDeceased || isArchived ? (
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
