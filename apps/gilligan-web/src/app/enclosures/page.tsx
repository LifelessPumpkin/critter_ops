import Link from "next/link";
import { EnclosureOverview } from "@/components/enclosures/EnclosureOverview";

export default function EnclosuresPage() {
  return (
    <>
      <section className="page-heading page-heading-with-action animated-fade-in">
        <div>
          <span className="hero-badge">Habitats</span>
          <h1 className="page-title">Enclosures</h1>
          <p className="page-subtitle">Browse tanks, cages, habitats, and other managed spaces in CritterOps.</p>
        </div>
        <Link href="/enclosures/new" className="button button-primary">
          Add Enclosure
        </Link>
      </section>

      <EnclosureOverview />
    </>
  );
}
