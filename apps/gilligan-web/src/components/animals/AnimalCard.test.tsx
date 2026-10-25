import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AnimalCard } from "@/components/animals/AnimalCard";
import type { Animal } from "@/lib/api/animals";

describe("AnimalCard", () => {
  it("renders animal details with the enclosure name instead of the raw enclosure id", () => {
    const animal: Animal = {
      id: 7,
      name: "Mango",
      species: "Ball Python",
      animalType: "Reptile",
      status: "Active",
      sex: "Female",
      enclosureId: 95,
      enclosureName: "Reptile Room Terrarium 1",
      acquiredDate: "2026-07-01",
    };

    render(<AnimalCard animal={animal} />);

    expect(screen.getByRole("heading", { name: "Mango" })).toBeInTheDocument();
    expect(screen.getByText("Ball Python")).toBeInTheDocument();
    expect(screen.getByText("Reptile Room Terrarium 1")).toBeInTheDocument();
    expect(screen.queryByText("95")).not.toBeInTheDocument();
  });
});

it("shows scientific identity and preserved original descriptions for migrated animals", () => {
  render(<AnimalCard animal={{ id: 8, name: "Migrated", enclosureId: 1, enclosureName: "Habitat", species: "Ball python", speciesId: 4,
    speciesScientificName: "Python regius", originalSpeciesText: "Historical species detail", animalType: "Reptile", animalGroupName: "Reptile",
    subspeciesOrMorph: "Albino", animalVariantId: 3, variantType: "Morph", originalVariantText: "Historical morph detail", status: "Active", sex: "Unknown", acquiredDate: "2026-01-01" }} />);
  expect(screen.getByText("Python regius")).toBeInTheDocument();
  expect(screen.getByText("Historical species detail")).toBeInTheDocument();
  expect(screen.getByText("Historical morph detail")).toBeInTheDocument();
});
