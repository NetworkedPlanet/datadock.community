using Datadock.Common.Models;
using FluentValidation;

namespace Datadock.Common.Validators
{
    public class JobInfoValidator : AbstractValidator<JobInfo>
    {
        public JobInfoValidator()
        {
            RuleFor(x => x.JobId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.RepositoryId).NotEmpty();
            RuleFor(x => x.GitRepositoryFullName).NotEmpty();
            RuleFor(x => x.GitRepositoryUrl).NotEmpty();
            RuleFor(x => x.JobType).IsInEnum();

        }
    }
}
