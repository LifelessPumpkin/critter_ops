# CritterOps Repository Structure and Labels

## GitHub Labels

CritterOps uses themed labels to organize work by project area. Each label maps to a major part of the system.

### skipper

Backend API work.

Use this label for issues involving the ASP.NET Core Web API, endpoints, request/response models, validation, authentication, service logic, and backend project setup.

Examples:

* Create animal API endpoints
* Add task completion endpoint
* Configure Swagger/OpenAPI
* Add backend validation

### professor

Database work.

Use this label for issues involving PostgreSQL, EF Core migrations, schema design, seed data, database relationships, and database documentation.

Examples:

* Create animal table
* Add enclosure relationship
* Write initial seed data
* Update database migration

### millionaire

Infrastructure and deployment work.

Use this label for issues involving Docker, Docker Swarm, networking, deployment files, environment variables, reverse proxy configuration, and production setup.

Examples:

* Add Dockerfile for Skipper API
* Create Docker Swarm stack file
* Configure overlay network
* Add environment variable templates

### gilligan

Frontend web app work.

Use this label for issues involving the Next.js application, pages, routing, data fetching, forms, dashboard screens, and browser-based user workflows.

Examples:

* Build animal list page
* Create today dashboard
* Add enclosure detail page
* Connect frontend to API

### ginger

Shared UI and design system work.

Use this label for reusable UI components, layout components, styling patterns, theme decisions, and shared frontend design elements.

Examples:

* Create Button component
* Create Card component
* Build app shell layout
* Define form styling patterns

## Why These Labels Exist

These labels make it easier to quickly understand what part of the system an issue affects. CritterOps has multiple moving pieces: frontend, backend, database, infrastructure, and shared UI. Without labels, issues can become hard to scan as the project grows.

The labels also make branch planning easier. For example, if an issue has the skipper label, the branch will likely touch apps/skipper-api. If an issue has the gilligan label, it will likely touch apps/gilligan-web.

Some issues may need multiple labels. For example, creating animal CRUD may involve:

* skipper for backend API work
* professor for database schema/migrations
* gilligan for frontend pages
* ginger if reusable UI components are created

## Repository Structure

```
critter-ops/
│
├── apps/
│   ├── gilligan-web/
│   └── skipper-api/
│
├── packages/
│   ├── ginger-ui/
│   └── professor-db/
│
├── millionaire/
│   ├── docker/
│   ├── nginx/
│   ├── scripts/
│   └── swarm/
│
├── documentation/
│   ├── specs/
│   ├── api/
│   ├── architecture/
│   └── decisions/
│
└── .github/
```
## How to Navigate the Repository

### apps/gilligan-web

The Next.js + TypeScript frontend application.

Go here when working on pages, routes, forms, dashboards, frontend API calls, and user-facing workflows.

### apps/skipper-api

The ASP.NET Core backend API.

Go here when working on API endpoints, backend services, validation, authentication, controllers, request DTOs, response DTOs, and application logic.

### packages/ginger-ui

The shared UI component library.

Go here when creating reusable frontend components like buttons, cards, inputs, badges, modals, layout components, and shared styling patterns.

### packages/professor-db

Database-related project assets.

Go here for database documentation, seed data, schema notes, migration documentation, and database helper scripts. EF Core migrations may live inside skipper-api, but database planning and documentation should live here.

### millionaire

Infrastructure and deployment configuration.

Go here for Dockerfiles, Docker Compose files, Docker Swarm stack files, deployment scripts, reverse proxy configuration, environment templates, and production deployment notes.

documentation
    
Project documentation.

Go here for product specs, architecture notes, API documentation, development decisions, and planning documents.

### .github

GitHub-specific configuration.

Go here for issue templates, pull request templates, GitHub Actions workflows, and repository automation.