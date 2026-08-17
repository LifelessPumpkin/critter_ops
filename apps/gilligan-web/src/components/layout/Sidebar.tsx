"use client";

import Link from "next/link";
import { primaryNavigation } from "@/components/layout/navigation";

type SidebarProps = {
  pathname: string;
};

export function Sidebar({ pathname }: SidebarProps) {
  return (
    <aside className="app-sidebar" aria-label="Primary navigation">
      <nav className="sidebar-nav">
        {primaryNavigation.map((item) => {
          const isActive =
            item.href === "/"
              ? pathname === "/"
              : pathname === item.href || pathname.startsWith(`${item.href}/`);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={isActive ? "sidebar-link sidebar-link-active" : "sidebar-link"}
              aria-current={isActive ? "page" : undefined}
            >
              <span className="sidebar-link-icon" aria-hidden="true">
                {item.icon}
              </span>
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
