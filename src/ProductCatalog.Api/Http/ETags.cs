using Microsoft.Net.Http.Headers;

namespace ProductCatalog.Api.Http;

public static class ETags
{
    private const string UnmatchableVersion = "unmatchable";

    public static string Format(string version) => $"\"{version}\"";

    public static string? ParseIfMatch(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return null;

        if (ifMatch.Trim() == "*")
            return null;

        if (!EntityTagHeaderValue.TryParse(ifMatch, out var etag))
            return UnmatchableVersion;

        if (etag.IsWeak)
            return UnmatchableVersion;

        return etag.Tag.Value!.Trim('"');
    }
}
