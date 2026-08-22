"use client";

import { useEffect, useState } from "react";
import { CollapsibleSection } from "@/components/common/CollapsibleSection";
import { EnclosureTable } from "@/components/enclosures/EnclosureTable";
import { type Enclosure, fetchEnclosures } from "@/lib/api/enclosures";

type LoadState = "loading" | "loaded" | "error";

export function EnclosureOverview() {
  const [enclosures, setEnclosures] = useState<Enclosure[]>([]);
  const [loadState, setLoadState] = useState<LoadState>("loading");

  useEffect(() => {
    let isMounted = true;

    async function loadEnclosures() {
      try {
        const enclosureData = await fetchEnclosures();

        if (isMounted) {
          setEnclosures(enclosureData);
          setLoadState("loaded");
        }
      } catch (error) {
        console.error(error);

        if (isMounted) {
          setLoadState("error");
        }
      }
    }

    loadEnclosures();

    return () => {
      isMounted = false;
    };
  }, []);

  if (loadState === "loading") {
    return <p className="state-message">Loading enclosures...</p>;
  }

  if (loadState === "error") {
    return (
      <div className="state-panel" role="alert">
        <h2>Unable to load enclosures.</h2>
        <p>Please try again later.</p>
      </div>
    );
  }

  if (enclosures.length === 0) {
    return (
      <div className="state-panel">
        <h2>No enclosures found.</h2>
        <p>Add your first enclosure to start tracking habitats.</p>
      </div>
    );
  }

  const locationGroups = groupEnclosuresByLocation(enclosures);

  return (
    <section className="location-section-list" aria-label="Enclosures by location">
      {locationGroups.map((group) => (
        <CollapsibleSection
          key={group.location}
          title={group.location}
          summary={`${group.enclosures.length} ${group.enclosures.length === 1 ? "enclosure" : "enclosures"}`}
        >
          <EnclosureTable enclosures={group.enclosures} />
        </CollapsibleSection>
      ))}
    </section>
  );
}

function groupEnclosuresByLocation(enclosures: Enclosure[]) {
  const groups = enclosures.reduce<Map<string, Enclosure[]>>((groupMap, enclosure) => {
    const location = enclosure.location.trim() || "Unassigned";
    const locationEnclosures = groupMap.get(location) ?? [];

    locationEnclosures.push(enclosure);
    groupMap.set(location, locationEnclosures);

    return groupMap;
  }, new Map());

  return Array.from(groups.entries())
    .map(([location, locationEnclosures]) => ({
      location,
      enclosures: locationEnclosures,
    }))
    .sort((firstGroup, secondGroup) => firstGroup.location.localeCompare(secondGroup.location));
}
