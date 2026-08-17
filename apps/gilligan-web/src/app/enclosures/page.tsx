import { EnclosureOverview } from "@/components/enclosures/EnclosureOverview";

export default function EnclosuresPage() {
  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Habitats</span>
        <h1 className="page-title">Enclosures</h1>
        <p className="page-subtitle">Browse tanks, cages, habitats, and other managed spaces in CritterOps.</p>
      </section>

      <EnclosureOverview />
    </>
  );
}
