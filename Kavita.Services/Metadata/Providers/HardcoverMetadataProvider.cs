using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Kavita.API.Services.Metadata;
using Kavita.Models.DTOs.Metadata;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Metadata.Providers;

public class HardcoverMetadataProvider : IBookMetadataProvider
{
    private readonly ILogger<HardcoverMetadataProvider> _logger;
    private const string GraphQlUrl = "https://api.hardcover.app/v1/graphql";

    public string ProviderName => "Hardcover";

    public HardcoverMetadataProvider(ILogger<HardcoverMetadataProvider> logger)
    {
        _logger = logger;
    }

    public async Task<ExternalBookMetadata?> FetchAsync(string title, string? author, string? apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        try
        {
            var query = BuildQuery(title);

            var response = await GraphQlUrl
                .WithTimeout(10)
                .WithHeader("Authorization", $"Bearer {apiKey}")
                .WithHeader("Content-Type", "application/json")
                .PostJsonAsync(new { query }, cancellationToken: ct)
                .ReceiveString();

            var result = JsonSerializer.Deserialize<HardcoverResponse>(response, JsonOptions);
            var book = result?.Data?.Books?.Length > 0 ? result.Data.Books[0] : null;
            if (book == null) return null;

            return new ExternalBookMetadata
            {
                CoverUrl = book.Image?.Url,
                Description = book.Description,
                Author = book.Contributions?[0]?.Author?.Name,
                ProviderName = ProviderName
            };
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            _logger.LogWarning("[Hardcover] Invalid API key. Please update it in Metadata Settings.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Hardcover] Failed to fetch metadata for '{Title}'", title);
            return null;
        }
    }

    private static string BuildQuery(string title) =>
        $@"{{
          books(where: {{title: {{_ilike: ""%{title}%""}}}}, limit: 1, order_by: {{users_count: desc}}) {{
            title
            description
            image {{ url }}
            contributions(where: {{role: {{_eq: ""author""}}}}, limit: 1) {{
              author {{ name }}
            }}
          }}
        }}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class HardcoverResponse
    {
        [JsonPropertyName("data")]
        public HardcoverData? Data { get; set; }
    }

    private sealed class HardcoverData
    {
        [JsonPropertyName("books")]
        public HardcoverBook[]? Books { get; set; }
    }

    private sealed class HardcoverBook
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("image")]
        public HardcoverImage? Image { get; set; }

        [JsonPropertyName("contributions")]
        public HardcoverContribution[]? Contributions { get; set; }
    }

    private sealed class HardcoverImage
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    private sealed class HardcoverContribution
    {
        [JsonPropertyName("author")]
        public HardcoverAuthor? Author { get; set; }
    }

    private sealed class HardcoverAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
