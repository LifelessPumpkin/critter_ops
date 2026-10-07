"use client";

import { usePathname } from "next/navigation";
import { Breadcrumbs } from "@/components/layout/Breadcrumbs";
import { Sidebar } from "@/components/layout/Sidebar";
import { TopNavigation } from "@/components/layout/TopNavigation";
import { normalizePath } from "@/components/layout/navigation";

type AppLayoutProps = {
  children: React.ReactNode;
};

export function AppLayout({ children }: AppLayoutProps) {
  const pathname = normalizePath(usePathname());

  return (
    <div className="app-shell">
      <TopNavigation />
      <div className="app-body">
        <Sidebar pathname={pathname} />
        <div className="app-content-shell">
          <Breadcrumbs pathname={pathname} />
          <main className="app-main">{children}</main>
        </div>
      </div>
    </div>
  );
}
