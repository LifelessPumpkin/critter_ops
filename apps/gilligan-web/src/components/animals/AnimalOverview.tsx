"use client";

import { useEffect, useState } from "react";
import { AnimalCard } from "@/components/animals/AnimalCard";
import { type Animal, fetchAnimals } from "@/lib/api/animals";

type LoadState = "loading" | "loaded" | "error";
type AnimalTab = "active" | "deceased";

const activeStatuses = new Set(["Active", "Quarantined", "Medical", "Breeding", "OnHold"]);

export function AnimalOverview() {
  const [animals, setAnimals] = useState<Animal[]>([]);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [selectedTab, setSelectedTab] = useState<AnimalTab>("active");

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

  const activeAnimals = animals.filter((animal) => activeStatuses.has(animal.status));
  const deceasedAnimals = animals.filter((animal) => animal.status === "Deceased");
  const visibleAnimals = selectedTab === "active" ? activeAnimals : deceasedAnimals;
  const emptyMessage = selectedTab === "active"
    ? "No active animals found."
    : "No deceased animal records found.";

  if (animals.length === 0) {
    return (
      <>
        <AnimalTabs
          activeCount={0}
          deceasedCount={0}
          selectedTab={selectedTab}
          onSelectTab={setSelectedTab}
        />
        <div className="state-panel">
          <h2>{emptyMessage}</h2>
          <p>Add your first animal to begin managing your collection.</p>
        </div>
      </>
    );
  }

  return (
    <>
      <AnimalTabs
        activeCount={activeAnimals.length}
        deceasedCount={deceasedAnimals.length}
        selectedTab={selectedTab}
        onSelectTab={setSelectedTab}
      />

      {visibleAnimals.length === 0 ? (
        <div className="state-panel">
          <h2>{emptyMessage}</h2>
        </div>
      ) : (
        <section className="enclosure-grid" aria-label={selectedTab === "active" ? "Active animals" : "Deceased animals"}>
          {visibleAnimals.map((animal) => (
            <AnimalCard key={animal.id} animal={animal} variant={selectedTab} />
          ))}
        </section>
      )}
    </>
  );
}

type AnimalTabsProps = {
  activeCount: number;
  deceasedCount: number;
  selectedTab: AnimalTab;
  onSelectTab: (tab: AnimalTab) => void;
};

function AnimalTabs({ activeCount, deceasedCount, selectedTab, onSelectTab }: AnimalTabsProps) {
  return (
    <div className="tab-list" role="tablist" aria-label="Animal status groups">
      <button
        className={selectedTab === "active" ? "tab-button tab-button-active" : "tab-button"}
        type="button"
        role="tab"
        aria-selected={selectedTab === "active"}
        onClick={() => onSelectTab("active")}
      >
        Active Animals <span>{activeCount}</span>
      </button>
      <button
        className={selectedTab === "deceased" ? "tab-button tab-button-active" : "tab-button"}
        type="button"
        role="tab"
        aria-selected={selectedTab === "deceased"}
        onClick={() => onSelectTab("deceased")}
      >
        Deceased Animals <span>{deceasedCount}</span>
      </button>
    </div>
  );
}
