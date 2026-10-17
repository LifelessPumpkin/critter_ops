import { AnimalForm } from "@/components/animals/AnimalForm";

export default async function NewAnimalPage({ searchParams }: { searchParams: Promise<{ workflow?: string; enclosureId?: string }> }) {
  const query = await searchParams;
  return (
    <>
      <section className="page-heading animated-fade-in">
        <h1 className="page-title">Add Animal</h1>
        <p className="page-subtitle">Create an animal record and assign its current enclosure.</p>
      </section>
      <AnimalForm key={query.workflow ?? "new"} workflow={query.workflow} enclosureId={Number(query.enclosureId) || undefined} />
    </>
  );
}
