using Datadock.Common.Models;
using FluentValidation;

namespace Datadock.Common.Validators
{
    public class RepoSettingsValidator: AbstractValidator<RepoSettings>
    {
        public RepoSettingsValidator()
        {
            RuleFor(os => os.OwnerId).NotEmpty();
        }
    }
}
