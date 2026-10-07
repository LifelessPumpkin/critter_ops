import { ActivityTimeline } from "@/components/activity/ActivityTimeline";
import { parseTimelineInitialState, type TimelineSearchParams } from "@/components/activity/timelineUrlState";

export default async function ActivityPage({ searchParams }: { searchParams: Promise<TimelineSearchParams> }) {
  const query = await searchParams;

  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">Operations</span>
        <h1 className="page-title">Activity</h1>
        <p className="page-subtitle">Review operational history across every animal and enclosure.</p>
      </section>
      <ActivityTimeline context={{ kind: "global" }} initialState={parseTimelineInitialState(query)} />
    </>
  );
}
