"use client";

import { useEffect, useState } from "react";
import { AnimalTable } from "@/components/animals/AnimalTable";
import { type Animal, fetchAnimals } from "@/lib/api/animals";

type LoadState = "loading" | "loaded" | "error";
type AnimalTab = "active" | "deceased" | "archived";

const activeStatuses = new Set(["Active", "Quarantined", "Medical", "Breeding", "OnHold"]);
const archivedStatuses = new Set(["Surrendered", "Transferred", "Sold", "Inactive", "Released"]);

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
  const archivedAnimals = animals.filter((animal) => archivedStatuses.has(animal.status));
  const visibleAnimals = getVisibleAnimals(selectedTab, activeAnimals, deceasedAnimals, archivedAnimals);
  const emptyMessage = getEmptyMessage(selectedTab);

  if (animals.length === 0) {
    return (
      <>
        <AnimalTabs
          activeCount={0}
          deceasedCount={0}
          archivedCount={0}
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
        archivedCount={archivedAnimals.length}
        selectedTab={selectedTab}
        onSelectTab={setSelectedTab}
      />

      {visibleAnimals.length === 0 ? (
        <div className="state-panel">
          <h2>{emptyMessage}</h2>
        </div>
      ) : (
        <AnimalTable animals={visibleAnimals} ariaLabel={getTabAriaLabel(selectedTab)} />
      )}
    </>
  );
}

type AnimalTabsProps = {
  activeCount: number;
  deceasedCount: number;
  archivedCount: number;
  selectedTab: AnimalTab;
  onSelectTab: (tab: AnimalTab) => void;
};

function AnimalTabs({ activeCount, deceasedCount, archivedCount, selectedTab, onSelectTab }: AnimalTabsProps) {
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
      <button
        className={selectedTab === "archived" ? "tab-button tab-button-active" : "tab-button"}
        type="button"
        role="tab"
        aria-selected={selectedTab === "archived"}
        onClick={() => onSelectTab("archived")}
      >
        Archived Animals <span>{archivedCount}</span>
      </button>
    </div>
  );
}

function getVisibleAnimals(
  selectedTab: AnimalTab,
  activeAnimals: Animal[],
  deceasedAnimals: Animal[],
  archivedAnimals: Animal[],
) {
  if (selectedTab === "deceased") {
    return deceasedAnimals;
  }

  if (selectedTab === "archived") {
    return archivedAnimals;
  }

  return activeAnimals;
}

function getEmptyMessage(selectedTab: AnimalTab) {
  if (selectedTab === "deceased") {
    return "No deceased animal records found.";
  }

  if (selectedTab === "archived") {
    return "No archived animal records found.";
  }

  return "No active animals found.";
}

function getTabAriaLabel(selectedTab: AnimalTab) {
  if (selectedTab === "deceased") {
    return "Deceased animals";
  }

  if (selectedTab === "archived") {
    return "Archived animals";
  }

  return "Active animals";
}
