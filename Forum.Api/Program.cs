// Forum.Api/Program.cs
using System.Text;
using Forum.Api.Common.Validation;
using Forum.Api.Data;
using Forum.Api.Features.Auth;
using Forum.Api.Features.Categories;
using Forum.Api.Features.Moderation;
using Forum.Api.Features.Notifications;
using Forum.Api.Features.Posts;
using Forum.Api.Features.Profiles;
using Forum.Api.Features.Search;
using Forum.Api.Features.Threads;
using Forum.Api.Features.Votes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration sources beyond the defaults ---
// Both load in EVERY environment on purpose: `dotnet ef` runs with no environment
// set (= Production), where the default builder skips user-secrets — and it still
// needs Jwt:Key and any machine-local connection-string override to build the host.
// appsettings.Development.local.json is gitignored; it's the demo-machine override
// (Docker SQL connection string + Jwt:Key) so committed config never changes.
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddUserSecrets<Program>(optional: true);

// --- Options ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    throw new InvalidOperationException(
        "Missing 'Jwt:Key'. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<64+ random chars>\" --project Forum.Api " +
        "— or put it in Forum.Api/appsettings.Development.local.json (gitignored).");

// --- Database (EF Core 9, SQL Server / LocalDB) ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Authentication / Authorization (manual JWT, no Identity scaffolding) ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // SignalR WebSocket connections cannot carry an Authorization header, so
        // hub clients pass the JWT as ?access_token= — accept it on hub paths only.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// --- MVC + global FluentValidation filter ---
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<ValidationFilter>();
});

// --- CORS (allow the Blazor Server host) ---
const string CorsPolicy = "ForumWebClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// --- OpenAPI ---
builder.Services.AddOpenApi();

// --- Feature slices ---
builder.Services.AddAuthFeature();
builder.Services.AddThreadsFeature();
builder.Services.AddPostsFeature();
builder.Services.AddVotesFeature();
builder.Services.AddProfilesFeature();
builder.Services.AddModerationFeature();
builder.Services.AddCategoriesFeature();
builder.Services.AddSearchFeature();
builder.Services.AddNotificationsFeature();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapNotificationsFeature();

app.Run();
