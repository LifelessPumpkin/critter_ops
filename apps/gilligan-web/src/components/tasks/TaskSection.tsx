import type { ReactNode } from "react";

type TaskSectionProps = {
  children: ReactNode;
  count?: number;
  title: string;
};

export function TaskSection({ children, count, title }: TaskSectionProps) {
  return (
    <section className="task-section">
      <div className="task-section-heading">
        <h2>{title}</h2>
        {count !== undefined ? <span>{count}</span> : null}
      </div>
      <div className="task-list">{children}</div>
    </section>
  );
}
