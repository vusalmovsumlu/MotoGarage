using System.Text.RegularExpressions;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Helpers;

public static class SlugHelper
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    { "my-profile", "search", "create", "update", "store", "my", "status", "stock", "cancel" };

    public static async Task<T> RetryAsync<T>(Func<Task<T>> create, string indexName)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { return await create(); }
            catch (DuplicateValueException ex) when (ex.IndexName == indexName && attempt < 4) { }
        }
    }

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

        value = string.IsNullOrWhiteSpace(value) ? "item" : value;
        if (Reserved.Contains(value)) value += "-item";
        return value.Length > 180 ? value[..180].TrimEnd('-') : value;
    }
}