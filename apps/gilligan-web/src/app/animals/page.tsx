import { AnimalOverview } from "@/components/animals/AnimalOverview";

export default function AnimalsPage() {
  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Collection</span>
        <h1 className="page-title">Animals</h1>
        <p className="page-subtitle">Review every animal currently tracked in CritterOps.</p>
      </section>

      <div className="toolbar-row">
        <label className="search-label" htmlFor="animal-search">
          Search animals
        </label>
        <input id="animal-search" className="search-input" placeholder="Search animals..." type="search" />
      </div>

      <AnimalOverview />
    </>
  );
}
