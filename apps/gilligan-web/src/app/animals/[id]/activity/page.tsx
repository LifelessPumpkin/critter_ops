import { EntityActivityPage } from "@/components/activity/EntityActivityPage";
import { parseTimelineInitialState, type TimelineSearchParams } from "@/components/activity/timelineUrlState";

export default async function AnimalActivityRoute({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<TimelineSearchParams>;
}) {
  const { id } = await params;
  const query = await searchParams;
  return <EntityActivityPage kind="animal" id={Number(id)} initialState={parseTimelineInitialState(query)} />;
}
