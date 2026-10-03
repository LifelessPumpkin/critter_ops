import { AnimalOverview } from "@/components/animals/AnimalOverview";

export default function AnimalsPage() {
  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Collection</span>
        <h1 className="page-title">Animals</h1>
        <p className="page-subtitle">Review every animal currently tracked in CritterOps.</p>
      </section>

      <AnimalOverview />
    </>
  );
}
