"use client";

import { useEffect, useState } from "react";
import { ActivityTimeline } from "@/components/activity/ActivityTimeline";
import { fetchAnimal, type Animal } from "@/lib/api/animals";
import { fetchEnclosure, type Enclosure } from "@/lib/api/enclosures";
import type { TimelineView } from "@/components/activity/timelineTypes";

type EntityActivityPageProps =
  | { kind: "animal"; id: number; initialDate?: string; initialView?: TimelineView }
  | { kind: "enclosure"; id: number; initialDate?: string; initialView?: TimelineView };

export function EntityActivityPage(props: EntityActivityPageProps) {
  const [entity, setEntity] = useState<Animal | Enclosure | null>(null);
  const [loadState, setLoadState] = useState<"loading" | "loaded" | "error">("loading");

  useEffect(() => {
    let isMounted = true;
    const request = props.kind === "animal" ? fetchAnimal(props.id) : fetchEnclosure(props.id);

    request
      .then((result) => {
        if (isMounted) {
          setEntity(result);
          setLoadState("loaded");
        }
      })
      .catch((error) => {
        console.error(error);
        if (isMounted) {
          setLoadState("error");
        }
      });

    return () => { isMounted = false; };
  }, [props.id, props.kind]);

  if (loadState === "loading") {
    return <p className="state-message">Loading {props.kind}...</p>;
  }

  if (loadState === "error" || !entity) {
    return (
      <div className="state-panel" role="alert">
        <h1>Unable to load {props.kind}.</h1>
        <p>The record may no longer exist or the service may be unavailable.</p>
      </div>
    );
  }

  const subtitle = props.kind === "animal"
    ? `${(entity as Animal).species} · ${(entity as Animal).enclosureName}`
    : `${(entity as Enclosure).type} · ${(entity as Enclosure).location}`;

  return (
    <>
      <section className="page-heading animated-fade-in">
        <span className="hero-badge">{props.kind === "animal" ? "Animal" : "Enclosure"}</span>
        <h1 className="page-title">{entity.name}</h1>
        <p className="page-subtitle">{subtitle}</p>
      </section>
      <ActivityTimeline
        context={{ kind: props.kind, id: props.id, name: entity.name }}
        initialDate={props.initialDate}
        initialView={props.initialView}
      />
    </>
  );
}
