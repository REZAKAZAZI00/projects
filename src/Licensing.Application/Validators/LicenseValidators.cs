using FluentValidation;
using Licensing.Application.Models;

namespace Licensing.Application.Validators;

public class CreateLicenseRequestValidator : AbstractValidator<CreateLicenseRequest>
{
    public CreateLicenseRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.MaxActivations).GreaterThan(0);
        RuleFor(x => x.ExpirationDateUtc).Must(d => d.Kind == DateTimeKind.Utc || d.Kind == DateTimeKind.Unspecified)
            .WithMessage("ExpirationDateUtc must be UTC.");
    }
}

public class ActivateLicenseRequestValidator : AbstractValidator<ActivateLicenseRequest>
{
    public ActivateLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.InstanceIdentifier).NotEmpty().MaximumLength(512);
    }
}

public class DeactivateLicenseRequestValidator : AbstractValidator<DeactivateLicenseRequest>
{
    public DeactivateLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.InstanceIdentifier).NotEmpty().MaximumLength(512);
    }
}

public class ValidateLicenseRequestValidator : AbstractValidator<ValidateLicenseRequest>
{
    public ValidateLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.InstanceIdentifier).MaximumLength(512).When(x => x.InstanceIdentifier is not null);
    }
}
