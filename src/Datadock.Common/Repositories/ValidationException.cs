﻿using System;
using System.Linq;
using FluentValidation.Results;

namespace Datadock.Common.Repositories
{
    public class ValidationException : DatadockException
    {
        private readonly ValidationResult _validationResult;
        public ValidationException(string baseMessage, ValidationResult validationResult) : base(baseMessage)
        {
            _validationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }

        public override string Message
        {
            get
            {
                var validationMessage = string.Join(" ",
                    _validationResult.Errors.Select(e => $"'{e.PropertyName}': {e.ErrorMessage}"));
                return base.Message + ": " + validationMessage;
            }
        }
    }
}