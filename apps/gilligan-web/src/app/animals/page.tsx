import Link from "next/link";
import { AnimalOverview } from "@/components/animals/AnimalOverview";

export default function AnimalsPage() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <Link href="/" className="logo-container">
          <div className="logo-icon">C</div>
          <span className="logo-text">CritterOps</span>
        </Link>
        <nav className="header-nav">
          <Link href="/animals" className="nav-link">
            Animals
          </Link>
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
          <span className="hero-badge">Collection</span>
          <h1 className="page-title">Animals</h1>
          <p className="page-subtitle">
            Review every animal currently tracked in CritterOps.
          </p>
        </section>

        <div className="toolbar-row">
          <label className="search-label" htmlFor="animal-search">
            Search animals
          </label>
          <input
            id="animal-search"
            className="search-input"
            placeholder="Search animals..."
            type="search"
          />
        </div>

        <AnimalOverview />
      </main>

      <footer className="app-footer">
        <p>© {new Date().getFullYear()} CritterOps. Development Console.</p>
      </footer>
    </div>
  );
}
