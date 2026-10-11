# 🎁 Secret Santa

A small Secret Santa app for family gift exchanges.

- **Organiser page** (`/admin`): create exchanges (name, date, description, people), add/remove people, draw names, and copy each person's private invite link. It never shows who has who.
- **Invite page** (`/invite/<token>`): each person opens their own link and reveals who they're buying for.

## Stack

| Folder | What |
| --- | --- |
| `web/` | React + TypeScript (Vite) |
| `api/` | .NET 10 minimal API + EF Core |
| — | PostgreSQL |

In production the .NET app serves the built React app, so it deploys as a single Railway service plus a Postgres database.

## Running locally

Prerequisites: .NET 10 SDK, Node 22+, and a local PostgreSQL.

```bash
# 1. API (http://localhost:5080). Uses the connection string and admin password
#    ("admin") in api/appsettings.Development.json; edit those to match your Postgres.
cd api
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
cd api && dotnet ef migrations add <Name> -o Data/Migrations
```

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
