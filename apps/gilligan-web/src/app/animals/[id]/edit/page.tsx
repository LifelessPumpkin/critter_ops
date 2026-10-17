import { AnimalForm } from "@/components/animals/AnimalForm";
export default async function EditAnimalPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <><h1 className="page-title">Edit Animal</h1><AnimalForm animalId={Number(id)} /></>;
}
