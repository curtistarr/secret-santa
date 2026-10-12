# 🎁 Secret Santa

A small Secret Santa app for family gift exchanges.

- **Organiser page** (`/admin`): create exchanges (name, date, description, people), add/remove people, draw names, and copy each person's private invite link. It never shows who has who.
- **Invite page** (`/invite/<token>`): each person opens their own link and reveals who they're buying for.

## Stack

| Folder | What |
| --- | --- |
| `web/` | React + TypeScript (Vite), tested with Vitest + React Testing Library |
| `api/SecretSanta.Api/` | .NET 10 Web API + EF Core |
| `api/SecretSanta.Api.Tests/` | NUnit unit tests for the API |
| — | PostgreSQL |

In production the .NET app serves the built React app, so it deploys as a single Railway service plus a Postgres database.

### API layers

The API is split into layers, each behind an interface so it can be mocked in tests or swapped out:

| Layer | Folder | Responsibility |
| --- | --- | --- |
| Controllers | `Controllers/` | HTTP only: routing, admin auth, and turning service results into responses. |
| Services | `Services/` | Business logic and orchestration: validation, drawing names, recording invite views. |
| Stores | `Stores/` | Simple CRUD, one store per table (`IExchangeStore`, `IParticipantStore`). |

Request and response shapes live in `Contracts/`, and EF Core entities and migrations in `Data/`.

## Running locally

Prerequisites: .NET 10 SDK, Node 22+, and a local PostgreSQL.

```bash
# 1. API (http://localhost:5080). Uses the connection string and admin password
#    ("admin") in api/SecretSanta.Api/appsettings.Development.json; edit those to match your Postgres.
cd api/SecretSanta.Api
dotnet run --urls http://localhost:5080

# 2. Frontend (http://localhost:5173), proxies /api to the API
cd web
npm install
npm run dev
```

The database schema is created/updated automatically on startup (EF Core migrations).

To add a migration after changing the models:

```bash
dotnet tool restore
cd api/SecretSanta.Api && dotnet ef migrations add <Name> -o Data/Migrations
```

## Tests and linting

```bash
# API: NUnit tests
cd api
dotnet test

# Frontend: lint, Vitest + React Testing Library tests, type check and build
cd web
npm run lint
npm test
npm run build
```

API controllers are tested against mocked services, and services against mocked stores (NSubstitute). Stores are tested against the EF Core in-memory provider.

Frontend tests sit next to the code they test (`*.test.ts(x)`). The API client is tested against a stubbed `fetch`, and each page component against a mocked API client.

All of these run on every push to `main` and on every pull request via GitHub Actions (`.github/workflows/ci.yml`).

## Deploying to Railway

1. Create a new Railway project and **Deploy from GitHub repo** → pick this repo. Railway finds `railway.json` and builds with the root `Dockerfile`.
2. Add a **PostgreSQL** database to the project.
3. On the app service, set these variables:
   - `DATABASE_URL` = `${{Postgres.DATABASE_URL}}` (reference variable to the database)
   - `ADMIN_PASSWORD` = a password for the organiser page
4. Under **Settings → Networking**, generate a public domain.
5. Visit `https://<your-domain>/admin`, log in, and create your first exchange.

## Configuration

| Variable | Purpose |
| --- | --- |
| `DATABASE_URL` | Postgres URL (`postgresql://user:pass@host:port/db`). Alternatively set `ConnectionStrings__Default`. |
| `ADMIN_PASSWORD` | Password for the organiser page. Admin endpoints are disabled if unset. |
| `PORT` | Port to listen on (set automatically by Railway). |

## How it works

- Each person gets a random, unguessable invite token (144 bits). Anyone with the link sees that person's page, so share links privately.
- Drawing shuffles everyone into a single loop and each person buys for the next, so no-one gets themselves.
- Admin API responses never include assignments. The admin can see whether each person has opened their link yet.
- Before drawing, links already work and say "names haven't been drawn yet". After drawing, people are locked; **Reset draw** clears assignments so you can change people and draw again (tell anyone who already looked).
