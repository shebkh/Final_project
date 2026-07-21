# Quorum

A discussion forum I built as my final project. Users register, start threads, reply, vote and build reputation; moderators keep things tidy. There's also a real-time notification layer on top — you get a toast when someone replies to your thread or votes on your stuff.

## Features

- User registration and login (JWT), public profiles with reputation score
- Categories with one level of sub-categories, managed by moderators
- Threads with Markdown bodies and up to 5 tags
- Replies with quote-reply
- Upvote/downvote on threads and replies
- Moderation: pin, lock, move between categories, delete
- Search by keyword and/or category
- Reputation = sum of votes on everything you wrote
- Interactive API docs (Scalar) with live testing
- Bonus: real-time notifications over SignalR (reply + vote toasts)

## Stack

- **API** — ASP.NET Core 9 Web API, EF Core 9 (code-first), SQL Server
- **Frontend** — Blazor Server, MudBlazor + a bit of Tailwind for layout
- **Auth** — JWT bearer done by hand, BCrypt for password hashing (no ASP.NET Identity)
- **Validation** — FluentValidation on the API, DataAnnotations on the forms
- **Markdown** — Markdig (bodies are stored raw, rendered on the web side)
- **Realtime** — SignalR
- **API docs** — Scalar (OpenAPI)

## Running it

You need the .NET 9 SDK and a SQL Server. LocalDB (comes with Visual Studio) works out of the box; Docker works too, see below.

```powershell
git clone https://github.com/shebkh/Final_project.git
cd Final_project

# the JWT signing key is not in the repo - set your own once:
dotnet user-secrets set "Jwt:Key" "any-random-string-of-64-or-more-characters" --project Forum.Api

# create the database
dotnet ef database update --project Forum.Api --startup-project Forum.Api

# run both (two terminals)
dotnet run --project Forum.Api --launch-profile https   # https://localhost:7294
dotnet run --project Forum.Web --launch-profile https   # https://localhost:7225
```

Open https://localhost:7225 — the landing page is at `/`, the forum starts at `/threads`.

### One-command setup (Docker)

On a fresh machine with the .NET 9 SDK, Docker Desktop (running) and git, `tools\setup-demo.ps1`
does the whole setup in one go — checks your tools, starts a SQL Server 2022 container, writes
`Forum.Api/appsettings.Development.local.json` with a fresh JWT key, and creates the database:

```powershell
.\tools\setup-demo.ps1
```

When it finishes it prints the exact commands to run the two apps and seed the demo data.

### Gotchas

- **Build fails with "file is locked by Forum.Web (####)"** — an app is still running and holding its `.exe`. Stop it (`Ctrl+C` in its terminal, or `taskkill /PID <pid> /F`), then rebuild.
- **Always use the `https` launch profile.** On the plain http port the API 307-redirects to https and some clients drop the `Authorization` header on the redirect.
- **`dotnet ef` runs as Production** (no environment set), so `appsettings.Development.local.json` and user-secrets are loaded in every environment on purpose. If migrations complain about a missing `Jwt:Key` or connection string, that local file is missing or empty.
- **Reset demo data:** the seed script skips users/threads that already exist, so it's safe to re-run. To start clean with Docker, delete the container (`docker rm -f quorum-sql`) and re-run `setup-demo.ps1`.

## API docs

With the API running, open https://localhost:7294/scalar for an interactive explorer of
every endpoint (grouped by feature). To test the protected ones, log in via
`POST /api/auth/login`, copy the token from the response, hit the Authorize button and
paste it in — then you can call create/vote/moderate endpoints straight from the page.

### Using Docker instead of LocalDB

```powershell
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=YourStrong_Pw1 -p 1433:1433 --name quorum-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Then create `Forum.Api/appsettings.Development.local.json` (it's gitignored, so your local setup never touches the committed config):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ForumDb;User Id=sa;Password=YourStrong_Pw1;TrustServerCertificate=True"
  },
  "Jwt": { "Key": "any-random-string-of-64-or-more-characters" }
}
```

(If you put `Jwt:Key` in this file you can skip the user-secrets step.) Then run `dotnet ef database update` and start the apps as above.

## Demo data

There's a script that fills the forum with a few users, categories, threads, replies and votes so it doesn't look empty:

```powershell
# API must be running first
.\tools\seed-demo.ps1                          # LocalDB
.\tools\seed-demo.ps1 -ConnectionString "..."  # anything else
```

Accounts it creates:

| user | password | role |
|------|----------|------|
| `admin` | `Admin_2026!` | moderator |
| `alex_dev` | `Demo_2026!` | user |
| `sara_m` | `Demo_2026!` | user |

## Making someone a moderator

Moderator is a flag on the user row, there's no UI for it:

```sql
UPDATE Users SET IsModerator = 1 WHERE UserName = 'whoever';
```

They have to log out and back in afterwards — the role lives in the JWT, so it only shows up on a fresh token.

## Notes

- The API and Web are separate projects talking over HTTP (typed clients on the web side). Ports are wired in `Forum.Web/appsettings.json` and the API's CORS config, so if you change one, change the other.
- To see the notifications working, open two browsers (or one normal + one private window), log in as two different users, and have one reply to the other's thread.
