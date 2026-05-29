// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// String field representing a market or exchange using ISO 10383 Market Identifier Code (MIC) values (see"Appendix 6-C).
/// </summary>
public class Exchange_t : String_t
{
    #region AtdlReferenceType<T> Overrides

    /// <summary>
    /// Validates the supplied value in terms of 'ISO 10383 correctness', i.e., MICs must be 4 characters in length.
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>ValidationResult indicating whether the supplied value is valid.</returns>
    protected override ValidationResult ValidateValue(string value, bool isRequired)
    {
        // A MIC is exactly 4 characters and cannot be all-whitespace (which would pass a bare
        // Length==4 check but is not a valid ISO 10383 code).
        if (value != null && (value.Length != 4 || string.IsNullOrWhiteSpace(value)))
        {
            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.InvalidExchangeCode);
        }

        if (isRequired && value == null)
        {
            return new ValidationResult(ValidationResult.ResultType.Missing, ErrorMessages.NonOptionalParameterNotSupplied2);
        }

        return ValidationResult.ValidResult;
    }

    /// <summary>
    /// Gets the human-readable type name for use in error messages shown to the user.
    /// </summary>
    /// <returns>Human-readable type name.</returns>
    protected override string GetHumanReadableTypeName()
    {
        return HumanReadableTypeNames.ExchangeType;
    }

    #endregion
}

