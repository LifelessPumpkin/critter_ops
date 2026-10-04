import { AnimalForm } from "@/components/animals/AnimalForm";

export default function NewAnimalPage() {
  return (
    <>
      <section className="page-heading animated-fade-in">
        <h1 className="page-title">Add Animal</h1>
        <p className="page-subtitle">Create an animal record and assign its current enclosure.</p>
      </section>
      <AnimalForm />
    </>
  );
}
