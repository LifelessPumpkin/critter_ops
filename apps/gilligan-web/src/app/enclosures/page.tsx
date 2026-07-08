import Link from "next/link";
import { EnclosureOverview } from "@/components/enclosures/EnclosureOverview";

export default function EnclosuresPage() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <Link href="/" className="logo-container">
          <div className="logo-icon">C</div>
          <span className="logo-text">CritterOps</span>
        </Link>
        <nav className="header-nav">
          <Link href="/enclosures" className="nav-link">
            Enclosures
          </Link>
          <a
            href="https://github.com/LifelessPumpkin/critter_ops"
            target="_blank"
            rel="noopener noreferrer"
            className="nav-link"
          >
            Repository
          </a>
        </nav>
      </header>

      <main className="app-main">
        <section className="page-heading animated-fade-in">
          <span className="hero-badge">Habitats</span>
          <h1 className="page-title">Enclosures</h1>
          <p className="page-subtitle">
            Browse tanks, cages, habitats, and other managed spaces in CritterOps.
          </p>
        </section>

        <EnclosureOverview />
      </main>

      <footer className="app-footer">
        <p>© {new Date().getFullYear()} CritterOps. Development Console.</p>
      </footer>
    </div>
  );
}
