namespace ProductCatalog.Api.Http;

public static class ETags
{
    public static string Format(string version) => $"\"{version}\"";

    // Returns the version the client expects, or null when there's no precondition (header absent or "*").
    // Only a single strong ETag is supported; anything else can never match, so it ends in a 412.
    public static string? ParseIfMatch(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return null;

        var value = ifMatch.Trim();

        if (value == "*")
            return null;

        return value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"') ? value[1..^1] : value;
    }
}
