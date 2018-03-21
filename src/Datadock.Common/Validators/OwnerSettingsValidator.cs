using Datadock.Common.Models;
using FluentValidation;

namespace Datadock.Common.Validators
{
    public class OwnerSettingsValidator: AbstractValidator<OwnerSettings>
    {
        public OwnerSettingsValidator()
        {
            RuleFor(os => os.OwnerId).NotEmpty();
        }
    }
}
