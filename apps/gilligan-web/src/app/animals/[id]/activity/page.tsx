import { EntityActivityPage } from "@/components/activity/EntityActivityPage";
import type { TimelineView } from "@/components/activity/timelineTypes";

export default async function AnimalActivityRoute({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ view?: string; date?: string }>;
}) {
  const { id } = await params;
  const query = await searchParams;
  return <EntityActivityPage kind="animal" id={Number(id)} initialView={parseView(query.view)} initialDate={parseDate(query.date)} />;
}

function parseView(value?: string): TimelineView {
  return value === "day" || value === "week" || value === "range" ? value : "list";
}

function parseDate(value?: string) {
  return value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : undefined;
}
