using Datadock.Common.Models;
using FluentValidation;

namespace Datadock.Common.Validators
{
    public class RepoSettingsValidator: AbstractValidator<RepoSettings>
    {
        public RepoSettingsValidator()
        {
            RuleFor(rs => rs.OwnerId).NotEmpty();
            RuleFor(rs => rs.RepositoryId).NotEmpty();
        }
    }
}
