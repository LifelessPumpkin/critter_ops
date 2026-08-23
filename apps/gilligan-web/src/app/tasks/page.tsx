import { TaskQueue } from "@/components/tasks/TaskQueue";

export default function TasksPage() {
  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Work Queue</span>
        <h1 className="page-title">Tasks</h1>
        <p className="page-subtitle">Track daily husbandry work, overdue care, and completed tasks.</p>
      </section>

      <TaskQueue />
    </>
  );
}
