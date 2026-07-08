// Forum.Api/Features/Categories/CategoriesModule.cs
using FluentValidation;

namespace Forum.Api.Features.Categories;

/// <summary>
/// Registers all services that belong to the Categories vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// </summary>
public static class CategoriesModule
{
    public static IServiceCollection AddCategoriesFeature(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryRequestValidator>();
        services.AddScoped<IValidator<UpdateCategoryRequest>, UpdateCategoryRequestValidator>();

        return services;
    }
}
