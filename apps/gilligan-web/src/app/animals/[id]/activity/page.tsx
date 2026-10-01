import { EntityActivityPage } from "@/components/activity/EntityActivityPage";

export default async function AnimalActivityRoute({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <EntityActivityPage kind="animal" id={Number(id)} />;
}
