import Link from "next/link";

export default function Home() {
  const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

  return (
    <div className="app-shell">
      {/* Header Layout Shell */}
      <header className="app-header">
        <div className="logo-container">
          <div className="logo-icon">C</div>
          <span className="logo-text">CritterOps</span>
        </div>
        <nav className="header-nav">
          <Link href="/enclosures" className="nav-link">
            Enclosures
          </Link>
          <a
            href="https://github.com/LifelessPumpkin/critter_ops"
            target="_blank"
            rel="noopener noreferrer"
            className="nav-link"
            id="nav-github-link"
          >
            Repository
          </a>
        </nav>
      </header>

      {/* Main Container */}
      <main className="app-main">
        {/* Hero Section */}
        <section className="hero-section animated-fade-in">
          <span className="hero-badge">Development Environment</span>
          <h1 className="hero-title">CritterOps Command Center</h1>
          <p className="hero-subtitle">
            Central dashboard linking backend services, telemetry, and interactive API documentation for developer operations.
          </p>
        </section>

        {/* Dashboard Grid containing Links */}
        <section className="dashboard-grid">
          {/* Swagger Card */}
          <div className="dashboard-card animated-fade-in animation-delay-1">
            <div>
              <div className="card-header-icon">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  width="24"
                  height="24"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z" />
                  <polyline points="14 2 14 8 20 8" />
                  <line x1="16" y1="13" x2="8" y2="13" />
                  <line x1="16" y1="17" x2="8" y2="17" />
                  <polyline points="10 9 9 9 8 9" />
                </svg>
              </div>
              <h2 className="card-title">Skipper API Specification</h2>
              <p className="card-description">
                Interactive Swagger / OpenAPI documentation for exploring, testing, and developing Skipper backend endpoints.
              </p>
            </div>
            <a
              href={`${apiBaseUrl}/swagger`}
              target="_blank"
              rel="noopener noreferrer"
              className="card-link"
              id="api-swagger-link"
            >
              Open API Docs <span className="arrow">→</span>
            </a>
          </div>

          {/* Health Check Telemetry Card */}
          <div className="dashboard-card animated-fade-in animation-delay-2">
            <div>
              <div className="card-header-icon">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  width="24"
                  height="24"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <path d="M22 12h-4l-3 9L9 3l-3 9H2" />
                </svg>
              </div>
              <h2 className="card-title">Service Telemetry</h2>
              <p className="card-description">
                Direct health check endpoint monitor verifying active server telemetry and connection status.
              </p>
            </div>
            <a
              href={`${apiBaseUrl}/health`}
              target="_blank"
              rel="noopener noreferrer"
              className="card-link"
              id="api-health-link"
            >
              Check Service Health <span className="arrow">→</span>
            </a>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="app-footer">
        <p>© {new Date().getFullYear()} CritterOps. Development Console.</p>
      </footer>
    </div>
  );
}
