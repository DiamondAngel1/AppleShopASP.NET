namespace AppleShop.Services;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static partial class Slugger
{
    public static string Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "item";

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);

        return NonAlphanumeric().Replace(builder.ToString(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}