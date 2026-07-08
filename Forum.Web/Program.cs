// Forum.Web/Program.cs
using Blazored.LocalStorage;
using Forum.Web.Components;
using Forum.Web.Features.Auth;
using Forum.Web.Features.Categories;
using Forum.Web.Features.Moderation;
using Forum.Web.Features.Search;
using Forum.Web.Features.Posts;
using Forum.Web.Features.Profiles;
using Forum.Web.Features.Threads;
using Forum.Web.Features.Votes;

var builder = WebApplication.CreateBuilder(args);

// Razor components (Blazor Server interactive).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Browser localStorage for token persistence.
builder.Services.AddBlazoredLocalStorage();

// Authorization core + cascading authentication state for <AuthorizeView> etc.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// Auth feature slice (token store, state provider, typed HttpClient).
builder.Services.AddAuthFeature(builder.Configuration);

// Threads feature slice (typed HttpClient; reuses AuthTokenHandler from Auth).
builder.Services.AddThreadsFeature(builder.Configuration);

// Posts feature slice (typed HttpClient; reuses AuthTokenHandler from Auth).
builder.Services.AddPostsFeature(builder.Configuration);

// Votes feature slice (typed HttpClient; reuses AuthTokenHandler from Auth).
builder.Services.AddVotesFeature(builder.Configuration);

// Profiles feature slice (typed HttpClient; public reads).
builder.Services.AddProfilesFeature(builder.Configuration);

// Moderation feature slice (typed HttpClient; moderator-only pin/lock/move).
builder.Services.AddModerationFeature(builder.Configuration);

// Categories feature slice (typed HttpClient; public reads, moderator-only management).
builder.Services.AddCategoriesFeature(builder.Configuration);

// Search feature slice (typed HttpClient; anonymous keyword/category search).
builder.Services.AddSearchFeature(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
