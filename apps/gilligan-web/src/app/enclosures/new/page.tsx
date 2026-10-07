import { EnclosureForm } from "@/components/enclosures/EnclosureForm";

export default function NewEnclosurePage() {
  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Habitats</span>
        <h1 className="page-title">Add Enclosure</h1>
        <p className="page-subtitle">Create a new habitat or enclosure.</p>
      </section>

      <EnclosureForm />
    </>
  );
}
