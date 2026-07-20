# Running Quorum on a fresh machine

Everything needed to get the project running for a demo. Two ways: the automated
script (recommended), or the manual steps if you want to see what it does.

## Prerequisites

- **.NET 9 SDK** — https://dotnet.microsoft.com/download (`dotnet --version` should print `9.x`)
- **Docker Desktop** — installed and *running* (the whale icon in the tray). Used for SQL Server.
- **Git**

The JWT signing key and the database connection string are **not** in the repo. They live in
`Forum.Api/appsettings.Development.local.json`, which is gitignored. The setup script creates it;
`appsettings.Development.local.example.json` shows the shape if you'd rather do it by hand.

## Option A — automated (recommended)

```powershell
git clone https://github.com/shebkh/Final_project.git
cd Final_project
.\tools\setup-demo.ps1
```

`setup-demo.ps1` checks your tools, starts the SQL container, writes the local config with a
fresh JWT key, and creates the database. When it finishes it prints the exact run + seed
commands. Follow them:

```powershell
# terminal 1
dotnet run --project Forum.Api --launch-profile https
# terminal 2
dotnet run --project Forum.Web --launch-profile https
# terminal 3 (after both are up)
.\tools\seed-demo.ps1 -ConnectionString "Server=localhost,1433;Database=ForumDb;User Id=sa;Password=Quorum_Demo2026!;TrustServerCertificate=True"
```

- Site: **https://localhost:7225**
- API docs (Scalar): **https://localhost:7294/scalar**

## Option B — manual

1. Start SQL Server:
   ```powershell
   docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Quorum_Demo2026! -p 1433:1433 --name quorum-sql -d mcr.microsoft.com/mssql/server:2022-latest
   ```
2. Create `Forum.Api/appsettings.Development.local.json` (copy from the `.example.json` next to it):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=ForumDb;User Id=sa;Password=Quorum_Demo2026!;TrustServerCertificate=True"
     },
     "Jwt": { "Key": "any-random-string-of-64-or-more-characters" }
   }
   ```
3. Create the database:
   ```powershell
   dotnet tool install --global dotnet-ef   # if you don't have it
   dotnet ef database update --project Forum.Api --startup-project Forum.Api
   ```
4. Run the two apps and seed, as in Option A.

## Demo logins

| user | password | role |
|------|----------|------|
| `admin` | `Admin_2026!` | moderator |
| `alex_dev` | `Demo_2026!` | user |
| `sara_m` | `Demo_2026!` | user |

## Gotchas (things that bit us before)

- **Build fails with "file is locked by Forum.Web (####)"** — an app is still running and holding
  its `.exe`. Stop it first: `Ctrl+C` in its terminal, or `taskkill /PID <pid> /F`. Then rebuild.
- **Always use the `https` launch profile.** On the plain http port the API 307-redirects to https
  and some clients drop the `Authorization` header on the redirect.
- **`dotnet ef` runs as Production** (no environment set), so `appsettings.Development.local.json`
  and user-secrets are loaded in *every* environment on purpose (see `Program.cs`). If migrations
  complain about a missing `Jwt:Key` or connection string, that local file is missing or empty.
- **Moderator role** is a DB flag baked into the JWT — after promoting a user you must log out and
  back in to get a token that carries the `Moderator` role. The seed script's `admin` is already a moderator.
- **Reset demo data:** the seed script is re-run safe (skips users/threads that already exist). To
  start clean, delete the container (`docker rm -f quorum-sql`) and re-run `setup-demo.ps1`.
