// Forum.Api/Features/Categories/CategoryValidators.cs
using FluentValidation;

namespace Forum.Api.Features.Categories;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 80).WithMessage("Name must be between 2 and 80 characters.");
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 80).WithMessage("Name must be between 2 and 80 characters.");
    }
}
