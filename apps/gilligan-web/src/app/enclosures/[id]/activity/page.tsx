import { EntityActivityPage } from "@/components/activity/EntityActivityPage";

export default async function EnclosureActivityRoute({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <EntityActivityPage kind="enclosure" id={Number(id)} />;
}
