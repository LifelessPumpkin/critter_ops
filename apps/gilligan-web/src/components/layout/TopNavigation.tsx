import Link from "next/link";

export function TopNavigation() {
  return (
    <header className="app-top-navigation">
      <Link href="/" className="app-brand" aria-label="CritterOps dashboard">
        <span className="app-brand-mark">C</span>
        <span className="app-brand-text">CritterOps</span>
      </Link>

      <div className="top-navigation-actions" aria-label="Global tools">
        <div className="global-search-placeholder" aria-hidden="true">
          Search
        </div>
        <div className="profile-placeholder" aria-hidden="true">
          LH
        </div>
      </div>
    </header>
  );
}
