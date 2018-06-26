﻿using Microsoft.Practices.EnterpriseLibrary.Validation;
using Plank.Net.Data;
using Plank.Net.Tests.Models;

namespace Plank.Net.Tests.Validators
{
    public class FailValidator : EntityValidator<ChildTwo>
    {
        #region METHODS

        public override ValidationResults Validate(ChildTwo item)
        {
            var result = new ValidationResults();
            result.AddResult(new ValidationResult("There was a problem", item, null, null, null));

            return result;
        }

        #endregion
    }
}
