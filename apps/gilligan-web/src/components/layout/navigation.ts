export type NavigationItem = {
  label: string;
  href: string;
  icon: string;
};

export const primaryNavigation: NavigationItem[] = [
  {
    label: "Dashboard",
    href: "/",
    icon: "D",
  },
  {
    label: "Animals",
    href: "/animals",
    icon: "A",
  },
  {
    label: "Enclosures",
    href: "/enclosures",
    icon: "E",
  },
  {
    label: "Tasks",
    href: "/tasks",
    icon: "T",
  },
  {
    label: "Calendar",
    href: "/calendar",
    icon: "C",
  },
];

export function getNavigationItem(pathname: string) {
  const normalizedPath = normalizePath(pathname);

  return primaryNavigation.find((item) => {
    if (item.href === "/") {
      return normalizedPath === "/";
    }

    return normalizedPath === item.href || normalizedPath.startsWith(`${item.href}/`);
  });
}

export function normalizePath(pathname: string) {
  if (!pathname) {
    return "/";
  }

  const pathWithoutQuery = pathname.split("?")[0] || "/";
  return pathWithoutQuery.length > 1 ? pathWithoutQuery.replace(/\/$/, "") : pathWithoutQuery;
}
