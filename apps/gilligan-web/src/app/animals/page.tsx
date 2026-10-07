import { AnimalOverview, type AnimalOverviewInitialState } from "@/components/animals/AnimalOverview";
import { ButtonLink } from "@/components/ui";

export default async function AnimalsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const parameters = await searchParams;
  const initialState: AnimalOverviewInitialState = {
    lifecycle: getSingleValue(parameters.lifecycle),
    search: getSingleValue(parameters.search),
    status: getSingleValue(parameters.status),
    type: getSingleValue(parameters.type),
    enclosure: getSingleValue(parameters.enclosure),
  };

  return (
    <>
      <section className="page-heading page-heading-with-action animal-page-heading animated-fade-in">
        <div>
          <h1 className="page-title">Animals</h1>
          <p className="page-subtitle">Manage animals currently and historically tracked in CritterOps.</p>
        </div>
        <ButtonLink href="/animals/new">+ Add Animal</ButtonLink>
      </section>

      <AnimalOverview initialState={initialState} />
    </>
  );
}

function getSingleValue(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}
