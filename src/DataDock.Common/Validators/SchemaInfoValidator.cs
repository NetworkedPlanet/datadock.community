using Datadock.Common.Models;
using FluentValidation;

namespace Datadock.Common.Validators
{
    public class SchemaInfoValidator : AbstractValidator<SchemaInfo>
    {
        public SchemaInfoValidator()
        {
            RuleFor(si => si.OwnerId).NotEmpty();
            RuleFor(si => si.RepositoryId).NotEmpty();
            RuleFor(si => si.SchemaId).NotEmpty();
        }
    }
}
