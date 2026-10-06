using System.Text.RegularExpressions;

namespace MotoGarage.Application.Helpers;

public static class SlugHelper
{
    public static string Generate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "item";
        }

        value = value
            .Replace("Ə", "E")
            .Replace("ə", "e")
            .Replace("Ğ", "G")
            .Replace("ğ", "g")
            .Replace("İ", "I")
            .Replace("ı", "i")
            .Replace("Ö", "O")
            .Replace("ö", "o")
            .Replace("Ü", "U")
            .Replace("ü", "u")
            .Replace("Ş", "S")
            .Replace("ş", "s")
            .Replace("Ç", "C")
            .Replace("ç", "c")
            .ToLowerInvariant()
            .Trim();

        value = Regex.Replace(
            value,
            @"[^a-z0-9\s-]",
            "");

        value = Regex.Replace(
            value,
            @"[\s-]+",
            "-");

        value = value.Trim('-');

        return string.IsNullOrWhiteSpace(value)
            ? "item"
            : value;
    }
}