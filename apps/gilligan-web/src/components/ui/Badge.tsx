import type { HTMLAttributes } from "react";

type BadgeVariant = "success" | "warning" | "error" | "info" | "neutral";

type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  variant?: BadgeVariant;
};

export function Badge({ className, variant = "neutral", ...props }: BadgeProps) {
  return <span className={["badge", `badge-${variant}`, className].filter(Boolean).join(" ")} {...props} />;
}

export function getStatusBadgeVariant(status: string): BadgeVariant {
  if (["Active", "Empty", "Reserved"].includes(status)) {
    return "success";
  }

  if (["UnderMaintenance", "CleaningRequired", "Quarantined", "Quarantine", "Medical", "OnHold"].includes(status)) {
    return "warning";
  }

  if (["Deceased", "Retired"].includes(status)) {
    return "error";
  }

  if (["Transferred", "Sold", "Released", "Surrendered"].includes(status)) {
    return "info";
  }

  return "neutral";
}
