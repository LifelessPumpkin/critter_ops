# CritterOps UI Structure & Navigation Specification

## Design Philosophy

CritterOps should feel like software built specifically for managing animals—not a generic CRUD admin panel.

The interface should prioritize clarity, speed, and organization. Users should be able to quickly understand the state of their animals, enclosures, and daily responsibilities without navigating through unnecessary screens.

### Core Design Principles

- Dashboard provides summaries, not management.
- Browse with visual layouts; manage with dense layouts.
- Organize information around physical spaces whenever possible.
- Minimize visual clutter through whitespace and clean typography.
- Use subtle colors inspired by Apple and Notion rather than modern dashboard aesthetics.
- Every page should allow users to quickly navigate elsewhere.

---

## Global Layout

Every primary page should share the same application layout.

```text
┌─────────────────────────────────────────────────────────────┐
│ Breadcrumbs                           Search     User Menu  │
├──────────────┬──────────────────────────────────────────────┤
│ Sidebar      │                                              │
│              │              Page Content                    │
│              │                                              │
│              │                                              │
└──────────────┴──────────────────────────────────────────────┘
```

---

## Sidebar

The sidebar serves as the primary navigation throughout the application.

Recommended structure:

- Dashboard
- Animals
- Enclosures
- Tasks
- Calendar
- Medical *(future)*
- Inventory *(future)*
- Reports *(future)*
- Settings

The sidebar should remain consistent across all pages.

---

## Top Bar

Every page should contain a top navigation bar.

### Left

Breadcrumb navigation.

Example:

```text
Dashboard

Dashboard > Animals

Dashboard > Animals > Ferret Nation

Dashboard > Animals > Ferret Nation > Noodle
```

Breadcrumbs should make navigation effortless and provide users with clear context.

### Right

Global Search

Notification Icon *(future)*

User Profile

Global search should eventually function similarly to Spotlight or Raycast, allowing users to search:

- Animals
- Enclosures
- Tasks
- Inventory
- Medical Records
- Reports

---

## Dashboard

Purpose:

Provide a quick overview of the current state of the collection.

The dashboard is **not** intended to manage records directly.

## Summary Cards

Examples:

- Active Animals
- Enclosures
- Animals in Medical
- Animals in Quarantine
- Tasks Due Today

These should immediately communicate the overall health of the collection.

---

## Today's Tasks

This section should display only a quick preview.

Example:

```text
Today's Tasks

□ Feed Ferrets

□ Clean Rabbit Hutch

□ Administer Medication

+ 2 more...
```

If all tasks are complete:

```text
All tasks completed for today.
```

The dashboard should encourage users to visit the Tasks page when needed rather than replace it.

---

## Weekly Preview

A compact overview showing upcoming work.

Example:

```text
This Week

Monday
3 Tasks

Tuesday
5 Tasks

Wednesday
2 Tasks
```

This is only a preview—not a full scheduling interface.

---

## Recent Activity *(Future)*

Examples:

- Animal Added
- Weight Recorded
- Medication Completed
- Enclosure Cleaned

---

## Animals Page

Purpose:

Efficiently manage a potentially large collection of animals.

This page prioritizes productivity over visual presentation.

## Layout

Top Toolbar

```text
Search...

Saved Views

Filters

+ Create Animal
```

---

## Search

Search by:

- Name
- Species
- Enclosure
- Identifier

---

## Saved Views

Users should be able to save frequently used filter combinations.

Examples:

- All Animals
- Medical
- Quarantine
- Reptiles
- Ferrets
- Newly Added

---

## Filters

Filters should open within a side drawer rather than permanently occupying screen space.

Potential filters:

- Species
- Status
- Enclosure
- Location
- Sex
- Age
- Date Added

Applied filters should appear as removable chips above the table.

---

## Animal Table

Primary interface:

| Photo | Name | Species | Sex | Status | Enclosure | Age |

Table columns should be sortable.

Rows should be clickable and open the Animal Details page.

---

## Create Animal

Selecting **+ Create Animal** should open a slide-over panel rather than navigating away from the page.

The slide-over should allow creation without disrupting the current workflow.

Future enhancement:

Multi-step creation if additional data becomes necessary.

---

## Animal Details

Individual animal pages should provide all information related to that animal.

Suggested tabs:

- Overview
- Medical
- Feeding History
- Weight History
- Documents
- Activity History

---

## Enclosures Page

Purpose:

Represent the physical layout of the collection.

The organization should mirror how someone walks through the facility.

## Structure

Enclosures should be grouped by location.

Examples:

```text
▼ Living Room

    Ferret Nation

    Rabbit Hutch

▼ Reptile Room

    Rack A

    Rack B

▼ Backyard

    Duck Pond

    Chicken Coop
```

Each location should be collapsible.

---

## Enclosure Cards

Each enclosure should be displayed as a card.

Suggested information:

- Name
- Animal Count
- Capacity
- Status
- Quick Health Indicators

Selecting a card opens the enclosure details page.

---

## Tasks

Purpose:

Provide a centralized location for recurring work.

Unlike the Dashboard, this page is intended for task management.

## Navigation

Use tabs rather than collapsible sections.

Suggested tabs:

- Today
- Upcoming
- Daily
- Weekly
- Monthly
- Custom
- Completed

---

## Task List

Each tab displays a task list.

Example:

```text
□ Feed Ferrets

□ Clean Pond

□ Replace Water

□ Medication
```

Tasks should support recurring schedules.

Examples:

- Every Day
- Every Friday
- Monday & Thursday
- Every 3 Months
- Every 6 Months

---

## Calendar

The Calendar should be its own page rather than embedded into Tasks.

Purpose:

Provide a visual scheduling interface.

Users should be able to:

- View scheduled tasks
- Browse future workload
- Identify busy days
- Open task details directly from calendar entries

The calendar complements the task list rather than replacing it.

---

## Navigation Flow

The application should support natural movement between related information.

Example:

```text
Dashboard

↓

Living Room

↓

Ferret Nation

↓

Noodle

↓

Medical History
```

Or

```text
Dashboard

↓

Tasks

↓

Medication

↓

Animal

↓

Enclosure
```

Users should rarely feel forced to return to the dashboard before reaching another page.

---

## Visual Style

The interface should emphasize calm, readable design.

### Colors

- White backgrounds
- Light gray borders
- Soft green primary accents
- Muted blues
- Minimal shadows

Avoid:

- Heavy gradients
- Bright neon colors
- Oversized cards
- Excessive animations

---

### Typography

Hierarchy should come primarily from spacing rather than bold colors.

Large amounts of whitespace should make the interface feel calm and uncluttered.

---

## Guiding Principle

Dashboard pages answer:

> "What do I need to know?"

Management pages answer:

> "What do I need to do?"

Maintaining this distinction should keep CritterOps fast, organized, and enjoyable to use as the collection grows from a handful of animals to hundreds.
