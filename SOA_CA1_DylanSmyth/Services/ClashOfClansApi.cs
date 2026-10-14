using System.Net;
using System.Text.Json;

namespace SOA_CA1_DylanSmyth.Services;

public sealed class ClashOfClansApi(HttpClient http, IConfiguration configuration)
{
    public async Task<JsonElement> GetPlayerAsync(string playerTag, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["ClashOfClans:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Add your Clash of Clans API key to user secrets or the ClashOfClans__ApiKey environment variable.");

        var tag = playerTag.Trim();
        if (!tag.StartsWith('#')) tag = "#" + tag;
        if (tag.Length < 4 || tag.Length > 15 || tag.Skip(1).Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException("Enter a valid player tag, such as #2ABC123.");

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.clashofclans.com/v1/players/{Uri.EscapeDataString(tag)}");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return document.RootElement.Clone();
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.NotFound => new InvalidOperationException("Player not found. Check the tag and try again."),
            HttpStatusCode.Forbidden => new InvalidOperationException("Clash of Clans rejected the API key. Check that it is valid and that this server's IP address is allowed in the developer portal."),
            HttpStatusCode.TooManyRequests => new InvalidOperationException("The API rate limit was reached. Wait a moment and try again."),
            _ => new InvalidOperationException($"Clash of Clans API returned {(int)response.StatusCode} ({response.ReasonPhrase}).")
        };
    }
}
