"use client";

import { useEffect, useState } from "react";
import { EnclosureCard } from "@/components/enclosures/EnclosureCard";
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

  return (
    <section className="enclosure-grid" aria-label="Enclosures">
      {enclosures.map((enclosure) => (
        <EnclosureCard key={enclosure.id} enclosure={enclosure} />
      ))}
    </section>
  );
}
