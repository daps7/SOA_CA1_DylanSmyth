using System.Text.Json;

namespace SOA_CA1_DylanSmyth.Components;

/// <summary>
/// Small helpers for showing values from the game API in the player pages.
/// </summary>
public static class PlayerProfileFormatting
{
    public static string GetText(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) ? value.ToString() : "-";
    }

    public static string GetNestedText(JsonElement element, string property, string nestedProperty)
    {
        if (element.TryGetProperty(property, out var nested) && nested.ValueKind == JsonValueKind.Object)
        {
            return GetText(nested, nestedProperty);
        }

        return "-";
    }

    public static string GetInitials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }
}
