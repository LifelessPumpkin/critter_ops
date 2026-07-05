# Professor DB (PostgreSQL Local Database)

This package contains assets, documentation, and tools related to the PostgreSQL database for the CritterOps application.

Local development database services are managed via Docker Compose at the root of the repository.

---

## Environment Variables

A `.env` file must exist in the root of the repository to define database configuration. You can copy the example file:

```bash
cp .env.example .env
```

The database configuration variables are:

| Variable | Description | Local Default |
| :--- | :--- | :--- |
| `POSTGRES_DB` | The name of the primary development database. | `critterops_dev` |
| `POSTGRES_USER` | The database username. | `postgres` |
| `POSTGRES_PASSWORD` | The password for the database user. | `postgres` |
| `POSTGRES_PORT` | The host port to expose PostgreSQL on (useful to change if port 5432 is already in use on your system). | `5432` |

---

## How to Manage the Database

All Docker Compose commands should be run from the **root directory** of the repository.

### Start the Database

To start the PostgreSQL container in the background (detached mode):

```bash
docker compose up -d
```

To verify that the database is running and check its logs:

```bash
docker compose logs -f db
```

### Stop the Database

To stop the database container without losing your data:

```bash
docker compose down
```

### Reset Local Database Data

To completely erase the local database and start fresh (this deletes the persistent named volume):

```bash
docker compose down -v
```

Then rebuild and start the database again:

```bash
docker compose up -d
```

---

## How to Connect Locally

You can connect to the database using any standard PostgreSQL client (e.g., `psql`, pgAdmin, DBeaver, or VS Code extensions).

### Connection Parameters

- **Host**: `localhost`
- **Port**: `<POSTGRES_PORT>` (default: `5432` or overridden value in `.env`, e.g., `5433`)
- **Database**: `<POSTGRES_DB>` (default: `critterops_dev`)
- **Username**: `<POSTGRES_USER>` (default: `postgres`)
- **Password**: `<POSTGRES_PASSWORD>` (default: `postgres`)

### Connection URL / Connection String

Using the default configuration:

```text
postgresql://postgres:postgres@localhost:5432/critterops_dev
```

If you configured a custom port (e.g., `5433`):

```text
postgresql://postgres:postgres@localhost:5433/critterops_dev
```

Using `psql` from the command line:

```bash
psql "postgresql://postgres:postgres@localhost:5432/critterops_dev"
```
