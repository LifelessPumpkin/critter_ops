import type { HTMLAttributes, ReactNode } from "react";

type CardProps = HTMLAttributes<HTMLElement> & {
  children: ReactNode;
};

type SummaryCardProps = Omit<CardProps, "children"> & {
  action?: ReactNode;
  description: string;
  icon?: ReactNode;
  title: string;
};

export function Card({ className, children, ...props }: CardProps) {
  return (
    <article className={["card", className].filter(Boolean).join(" ")} {...props}>
      {children}
    </article>
  );
}

export function SummaryCard({ action, description, icon, title, className, ...props }: SummaryCardProps) {
  return (
    <Card className={["summary-card", className].filter(Boolean).join(" ")} {...props}>
      <div>
        {icon ? <div className="card-header-icon">{icon}</div> : null}
        <h2 className="card-title">{title}</h2>
        <p className="card-description">{description}</p>
      </div>
      {action}
    </Card>
  );
}
