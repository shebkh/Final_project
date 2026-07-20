// Forum.Api/Common/OpenApi/JwtSecuritySchemeTransformer.cs
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Forum.Api.Common.OpenApi;

/// <summary>
/// Adds a "Bearer" JWT security scheme to the generated OpenAPI document and applies
/// it to the whole API. This is what gives Scalar its Authorize button — paste a token
/// from the login response there and the protected endpoints can be tested live.
/// </summary>
public sealed class JwtSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the JWT returned by /api/auth/login (just the token, no \"Bearer \" prefix)."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        // Require the scheme document-wide; anonymous endpoints still work without a token,
        // this only tells the UI which endpoints accept one.
        var reference = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement { [reference] = [] });

        document.Info = new OpenApiInfo
        {
            Title = "Quorum API",
            Version = "v1",
            Description = "REST API for Quorum, a discussion forum: auth, threads, replies, "
                        + "votes, categories, search, moderation and reputation."
        };

        return Task.CompletedTask;
    }
}
