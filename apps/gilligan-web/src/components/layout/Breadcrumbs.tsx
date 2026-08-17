import Link from "next/link";
import { primaryNavigation } from "@/components/layout/navigation";

type BreadcrumbsProps = {
  pathname: string;
};

type Breadcrumb = {
  label: string;
  href: string;
};

export function Breadcrumbs({ pathname }: BreadcrumbsProps) {
  const breadcrumbs = getBreadcrumbs(pathname);

  return (
    <nav className="breadcrumbs" aria-label="Breadcrumb">
      <ol>
        {breadcrumbs.map((breadcrumb, index) => {
          const isCurrent = index === breadcrumbs.length - 1;

          return (
            <li key={breadcrumb.href}>
              {isCurrent ? (
                <span aria-current="page">{breadcrumb.label}</span>
              ) : (
                <Link href={breadcrumb.href}>{breadcrumb.label}</Link>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

function getBreadcrumbs(pathname: string): Breadcrumb[] {
  const segments = pathname.split("/").filter(Boolean);

  if (segments.length === 0) {
    return [{ label: "Dashboard", href: "/" }];
  }

  return segments.map((segment, index) => {
    const href = `/${segments.slice(0, index + 1).join("/")}`;
    const navigationItem = primaryNavigation.find((item) => item.href === href);

    return {
      label: navigationItem?.label ?? formatSegment(segment),
      href,
    };
  });
}

function formatSegment(segment: string) {
  return segment
    .split("-")
    .filter(Boolean)
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(" ");
}
