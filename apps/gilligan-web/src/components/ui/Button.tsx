import Link from "next/link";
import type { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode } from "react";

type ButtonVariant = "primary" | "secondary" | "danger" | "ghost";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
};

type ButtonLinkProps = AnchorHTMLAttributes<HTMLAnchorElement> & {
  children: ReactNode;
  href: string;
  variant?: ButtonVariant;
};

export function Button({ className, variant = "primary", ...props }: ButtonProps) {
  return <button className={getButtonClassName(variant, className)} {...props} />;
}

export function ButtonLink({ className, variant = "primary", ...props }: ButtonLinkProps) {
  return <Link className={getButtonClassName(variant, className)} {...props} />;
}

function getButtonClassName(variant: ButtonVariant, className?: string) {
  return ["button", `button-${variant}`, className].filter(Boolean).join(" ");
}
