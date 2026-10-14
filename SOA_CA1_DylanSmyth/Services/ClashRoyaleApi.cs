using System.Net;
using System.Text.Json;

namespace SOA_CA1_DylanSmyth.Services;

public sealed class ClashRoyaleApi(HttpClient http, IConfiguration configuration)
{
    public async Task<JsonElement> GetPlayerAsync(string playerTag, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["ClashRoyale:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Add your Clash Royale API key to user secrets or the ClashRoyale__ApiKey environment variable.");

        var tag = playerTag.Trim().TrimStart('#');
        const string validCharacters = "0289PYLQGRJCUV";
        if (tag.Length is < 3 or > 15 || tag.Any(c => !validCharacters.Contains(char.ToUpperInvariant(c))))
            throw new ArgumentException("Enter a valid Clash Royale player tag, such as #2ABC123.");
        tag = "#" + tag.ToUpperInvariant();

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.clashroyale.com/v1/players/{Uri.EscapeDataString(tag)}");
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
            HttpStatusCode.Forbidden => new InvalidOperationException("Clash Royale rejected the API key. Check that it is valid and that this server's IP address is allowed in the developer portal."),
            HttpStatusCode.TooManyRequests => new InvalidOperationException("The API rate limit was reached. Wait a moment and try again."),
            _ => new InvalidOperationException($"Clash Royale API returned {(int)response.StatusCode} ({response.ReasonPhrase}).")
        };
    }
}
