using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Common;

/// <summary>Indian mobile numbers: stored as the 10 digits, whatever spacing or +91 was typed.</summary>
public static class Phones
{
    /// <summary>"98765 43210", "+91-98765-43210", "098765 43210" all become "9876543210"; null if not a mobile number.</summary>
    public static string? NormalizeMobile(string? value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91")) digits = digits[2..];
        else if (digits.Length == 11 && digits.StartsWith('0')) digits = digits[1..];
        return digits.Length == 10 && digits[0] is >= '6' and <= '9' ? digits : null;
    }
}

/// <summary>Rejects anything that isn't a 10-digit Indian mobile number (spaces, dashes and +91 are fine).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IndianMobileAttribute : ValidationAttribute
{
    public IndianMobileAttribute() : base("{0} must be a 10-digit mobile number.") { }

    public override bool IsValid(object? value) =>
        value is null || value is string s && (string.IsNullOrWhiteSpace(s) || Phones.NormalizeMobile(s) is not null);
}
