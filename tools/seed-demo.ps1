# Seeds demo data for Quorum through the API.
# Works against any environment: run the API first, then this script.
#   .\tools\seed-demo.ps1                          -> LocalDB dev setup
#   .\tools\seed-demo.ps1 -ConnectionString "..."  -> demo machine (Docker SQL)
# The connection string is only used for one thing the API can't do:
# flipping IsModerator on the admin account.
param(
    [string]$ApiBase = "https://localhost:7294",
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=ForumDb;Trusted_Connection=True;TrustServerCertificate=True"
)

$ErrorActionPreference = 'Stop'

# demo accounts (also listed in the README)
$accounts = @(
    @{ user = 'admin';    pass = 'Admin_2026!' },
    @{ user = 'alex_dev'; pass = 'Demo_2026!' },
    @{ user = 'sara_m';   pass = 'Demo_2026!' }
)

function Invoke-Api($method, $path, $token, $body) {
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    $args = @{ Method = $method; Uri = "$ApiBase$path"; Headers = $headers }
    if ($null -ne $body) {
        $args.ContentType = 'application/json'
        $args.Body = ($body | ConvertTo-Json -Depth 5)
    }
    Invoke-RestMethod @args
}

# --- users: register, or log in if they already exist ---
$tokens = @{}
foreach ($a in $accounts) {
    try {
        $r = Invoke-Api Post '/api/auth/register' $null @{ userName = $a.user; email = "$($a.user)@quorum.local"; password = $a.pass }
        Write-Host "registered $($a.user)"
    } catch {
        $r = Invoke-Api Post '/api/auth/login' $null @{ userNameOrEmail = $a.user; password = $a.pass }
        Write-Host "$($a.user) already exists, logged in"
    }
    $tokens[$a.user] = $r.token
}

# --- promote admin to moderator (needs DB, then a fresh login for the role claim) ---
$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "UPDATE Users SET IsModerator = 1 WHERE UserName = 'admin'"
[void]$cmd.ExecuteNonQuery()
$conn.Close()
$tokens['admin'] = (Invoke-Api Post '/api/auth/login' $null @{ userNameOrEmail = 'admin'; password = 'Admin_2026!' }).token
Write-Host "admin promoted to moderator"

# --- categories: create the ones that are missing ---
$existing = @(Invoke-Api Get '/api/categories' $null $null)
function Ensure-Category($name, $parentId) {
    $found = $existing | Where-Object { $_.name -eq $name } | Select-Object -First 1
    if ($found) { return $found.id }
    $c = Invoke-Api Post '/api/categories' $tokens['admin'] @{ name = $name; parentId = $parentId }
    Write-Host "created category $name"
    return $c.id
}
$catGeneral = Ensure-Category 'General' $null
$catIntros  = Ensure-Category 'Introductions' $catGeneral
$catDev     = Ensure-Category 'Development' $null

# --- threads (skipped when a thread with the same title is already there) ---
$page = Invoke-Api Get '/api/threads?page=1&pageSize=50' $null $null
$existingTitles = @($page.items | ForEach-Object { $_.title })

function New-Thread($token, $title, $body, $categoryId, $tags) {
    if ($existingTitles -contains $title) {
        Write-Host "skip thread (exists): $title"
        return $null
    }
    $t = Invoke-Api Post '/api/threads' $token @{ title = $title; body = $body; categoryId = $categoryId; tags = $tags }
    Write-Host "created thread: $title"
    return $t
}

$welcome = New-Thread $tokens['admin'] 'Welcome to Quorum - introduce yourself' @'
Hey everyone, welcome!

This is the place to say hi. A few ideas to get you started:

- what you're working on right now
- what you want to learn
- your favorite language (wrong answers accepted)

Keep it friendly.
'@ $catIntros @('welcome', 'community')

$blazor = New-Thread $tokens['alex_dev'] 'What I learned building my first Blazor Server app' @'
Just finished my first real Blazor Server project and wanted to share a few things I wish I knew earlier.

**1. Render modes matter.** If a page shows per-user data, turn prerendering off or you get weird flashes of the wrong state.

**2. `@code` blocks grow fast.** Move anything past ~50 lines into a partial class, future you will thank you.

**3. EventCallback beats cascading parameters** for child-to-parent communication in almost every case I hit.

What tripped *you* up when you started?
'@ $catDev @('blazor', 'dotnet', 'lessons')

$efcore = New-Thread $tokens['sara_m'] 'Is this EF Core query doing two round trips?' @'
Quick sanity check. I have this:

```csharp
var items = await db.Posts.Where(p => p.ThreadId == id).ToListAsync();
var total = await db.Posts.CountAsync(p => p.ThreadId == id);
```

Two awaits = two round trips, right? Is there a clean way to get the page and the total in one go, or is everyone just living with this?
'@ $catDev @('ef-core', 'sql', 'performance')

$tools = New-Thread $tokens['alex_dev'] 'Favorite VS Code extensions for C# work?' @'
Setting up a fresh machine. So far I have the C# Dev Kit and GitLens. What else is actually worth installing? Trying to keep it lean this time instead of hoarding 40 extensions I never use.
'@ $catDev @('tooling', 'vscode')

$rules = New-Thread $tokens['admin'] 'Forum rules - read before posting' @'
Short version:

1. Be respectful. Disagree with ideas, not people.
2. Search before posting - your question might already have an answer.
3. Use tags so people can find your thread.
4. No spam, no self-promotion without context.

This thread is locked. If you have questions about the rules, message a moderator.
'@ $catGeneral @('rules')

# --- replies + votes (only on threads created in this run, so re-running is safe) ---
if ($welcome) {
    [void](Invoke-Api Post "/api/threads/$($welcome.id)/posts" $tokens['alex_dev'] @{ body = "Hi, I'm Alex. Mostly C# and a bit of TypeScript. Currently fighting with Blazor render modes and mostly winning." })
    [void](Invoke-Api Post "/api/threads/$($welcome.id)/posts" $tokens['sara_m'] @{ body = "Sara here. Backend person, SQL enjoyer. Here to steal, I mean learn, EF Core tricks." })
    foreach ($u in 'alex_dev', 'sara_m') { [void](Invoke-Api Put "/api/threads/$($welcome.id)/vote" $tokens[$u] @{ value = 1 }) }
}

if ($blazor) {
    $reply = Invoke-Api Post "/api/threads/$($blazor.id)/posts" $tokens['sara_m'] @{ body = "The prerender flash got me too. Took me a whole evening to figure out it wasn't a bug in my code." }
    # quote-reply, same format the Quote button produces in the UI
    [void](Invoke-Api Post "/api/threads/$($blazor.id)/posts" $tokens['alex_dev'] @{ body = "> sara_m wrote:`n> The prerender flash got me too. Took me a whole evening to figure out it wasn't a bug in my code.`n`nSame! The docs mention it but only in passing. Should honestly be a big red box on the first page." })
    [void](Invoke-Api Put "/api/threads/$($blazor.id)/vote" $tokens['sara_m'] @{ value = 1 })
    [void](Invoke-Api Put "/api/threads/$($blazor.id)/vote" $tokens['admin'] @{ value = 1 })
    [void](Invoke-Api Put "/api/posts/$($reply.id)/vote" $tokens['alex_dev'] @{ value = 1 })
}

if ($efcore) {
    $reply = Invoke-Api Post "/api/threads/$($efcore.id)/posts" $tokens['alex_dev'] @{ body = "Yes, two round trips. Common fix is returning both from one service call as a paged result object - one query for the page, one for the count, but at least behind a single method so callers can't forget the count." }
    [void](Invoke-Api Put "/api/threads/$($efcore.id)/vote" $tokens['alex_dev'] @{ value = 1 })
    [void](Invoke-Api Put "/api/posts/$($reply.id)/vote" $tokens['sara_m'] @{ value = 1 })
    [void](Invoke-Api Put "/api/posts/$($reply.id)/vote" $tokens['admin'] @{ value = 1 })
}

if ($tools) {
    [void](Invoke-Api Put "/api/threads/$($tools.id)/vote" $tokens['sara_m'] @{ value = 1 })
}

# --- moderation showcase: pin the welcome thread, lock the rules thread ---
if ($welcome) { [void](Invoke-Api Put "/api/moderation/threads/$($welcome.id)/pin" $tokens['admin'] @{ pinned = $true }) }
if ($rules)   { [void](Invoke-Api Put "/api/moderation/threads/$($rules.id)/lock" $tokens['admin'] @{ locked = $true }) }

Write-Host ""
Write-Host "done. demo accounts:"
Write-Host "  admin    / Admin_2026!  (moderator)"
Write-Host "  alex_dev / Demo_2026!"
Write-Host "  sara_m   / Demo_2026!"
