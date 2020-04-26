﻿using Microsoft.Practices.EnterpriseLibrary.Validation;
using Plank.Net.Tests.Models;
using Plank.Net.Validators;

namespace Plank.Net.Tests.Validators
{
    public class FailGrandParentValidator : EntityValidator<GrandParentEntity>
    {
        #region METHODS

        public override ValidationResults Validate(GrandParentEntity item)
        {
            var result = new ValidationResults();
            result.AddResult(new ValidationResult("There was a problem", item, null, null, null));

            return result;
        }

        #endregion
    }
}
