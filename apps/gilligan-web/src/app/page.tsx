import Link from "next/link";
import { SummaryCard } from "@/components/ui";

export default function Home() {
  const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5198";

  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Development Environment</span>
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">
          Central dashboard linking backend services, telemetry, and interactive API documentation for developer operations.
        </p>
      </section>

      <section className="dashboard-grid">
        <SummaryCard
          action={
            <Link href={`${apiBaseUrl}/swagger`} target="_blank" rel="noopener noreferrer" className="card-link">
              Open API Docs <span className="arrow">-&gt;</span>
            </Link>
          }
          className="animated-fade-in animation-delay-1"
          description="Interactive Swagger / OpenAPI documentation for exploring, testing, and developing Skipper backend endpoints."
          icon={
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
          }
          title="Skipper API Specification"
        />

        <SummaryCard
          action={
            <Link href={`${apiBaseUrl}/health`} target="_blank" rel="noopener noreferrer" className="card-link">
              Check Service Health <span className="arrow">-&gt;</span>
            </Link>
          }
          className="animated-fade-in animation-delay-2"
          description="Direct health check endpoint monitor verifying active server telemetry and connection status."
          icon={
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
          }
          title="Service Telemetry"
        />
      </section>
    </>
  );
}
