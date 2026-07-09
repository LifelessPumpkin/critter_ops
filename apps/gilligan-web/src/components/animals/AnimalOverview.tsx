"use client";

import { useEffect, useState } from "react";
import { AnimalCard } from "@/components/animals/AnimalCard";
import { type Animal, fetchAnimals } from "@/lib/api/animals";

type LoadState = "loading" | "loaded" | "error";

export function AnimalOverview() {
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [loadState, setLoadState] = useState<LoadState>("loading");

  useEffect(() => {
    let isMounted = true;

    async function loadAnimals() {
      try {
        const animalData = await fetchAnimals();

        if (isMounted) {
          setAnimals(animalData);
          setLoadState("loaded");
        }
      } catch (error) {
        console.error(error);

        if (isMounted) {
          setLoadState("error");
        }
      }
    }

    loadAnimals();

    return () => {
      isMounted = false;
    };
  }, []);

  if (loadState === "loading") {
    return <p className="state-message">Loading animals...</p>;
  }

  if (loadState === "error") {
    return (
      <div className="state-panel" role="alert">
        <h2>Unable to load animals.</h2>
        <p>Please try again later.</p>
      </div>
    );
  }

  if (animals.length === 0) {
    return (
      <div className="state-panel">
        <h2>No animals found.</h2>
        <p>Add your first animal to begin managing your collection.</p>
      </div>
    );
  }

  return (
    <section className="enclosure-grid" aria-label="Animals">
      {animals.map((animal) => (
        <AnimalCard key={animal.id} animal={animal} />
      ))}
    </section>
  );
}
