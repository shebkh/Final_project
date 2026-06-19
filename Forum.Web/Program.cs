// Forum.Web/Program.cs
using Blazored.LocalStorage;
using Forum.Web.Components;
using Forum.Web.Features.Auth;
using Forum.Web.Features.Threads;

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
