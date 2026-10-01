import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AnimalCard } from "@/components/animals/AnimalCard";
import type { Animal } from "@/lib/api/animals";

describe("AnimalCard", () => {
  it("renders animal details with the enclosure name instead of the raw enclosure id", () => {
    const animal: Animal = {
      id: "7f5f7f42-8b14-4a24-8e70-6b9db00e6e9f",
      name: "Mango",
      species: "Ball Python",
      animalType: "Reptile",
      status: "Active",
      sex: "Female",
      enclosureId: "95ec40f0-c6bb-4231-8af0-0e2b25a394ea",
      enclosureName: "Reptile Room Terrarium 1",
      acquiredDate: "2026-07-01",
    };

    render(<AnimalCard animal={animal} />);

    expect(screen.getByRole("heading", { name: "Mango" })).toBeInTheDocument();
    expect(screen.getByText("Ball Python")).toBeInTheDocument();
    expect(screen.getByText("Reptile Room Terrarium 1")).toBeInTheDocument();
    expect(screen.queryByText("95ec40f0-c6bb-4231-8af0-0e2b25a394ea")).not.toBeInTheDocument();
  });
});
