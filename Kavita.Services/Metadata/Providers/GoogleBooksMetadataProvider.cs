using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Kavita.API.Services.Metadata;
using Kavita.Models.DTOs.Metadata;
using Kavita.Models.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Metadata.Providers;

public class GoogleBooksMetadataProvider : IBookMetadataProvider
{
    private readonly ILogger<GoogleBooksMetadataProvider> _logger;
    private const string SearchUrl = "https://www.googleapis.com/books/v1/volumes";

    public string ProviderName => "Google Books";

    public GoogleBooksMetadataProvider(ILogger<GoogleBooksMetadataProvider> logger)
    {
        _logger = logger;
    }

    public async Task<ExternalBookMetadata?> FetchAsync(string title, string? author, string? apiKey, CancellationToken ct = default)
    {
        try
        {
            var query = $"intitle:{title}";
            if (!string.IsNullOrWhiteSpace(author))
                query += $"+inauthor:{author}";

            var request = SearchUrl
                .SetQueryParam("q", query)
                .SetQueryParam("maxResults", 1)
                .SetQueryParam("printType", "books");

            if (!string.IsNullOrWhiteSpace(apiKey))
                request = request.SetQueryParam("key", apiKey);

            var response = await request
                .WithTimeout(10)
                .GetStringAsync(cancellationToken: ct);

            var result = JsonSerializer.Deserialize<GoogleBooksResult>(response, JsonOptions);
            var item = result?.Items?.Length > 0 ? result.Items[0] : null;
            if (item?.VolumeInfo == null) return null;

            var info = item.VolumeInfo;

            // Upgrade thumbnail to a larger image by removing zoom/edge params
            var coverUrl = info.ImageLinks?.Thumbnail?
                .Replace("zoom=1", "zoom=0")
                .Replace("&edge=curl", string.Empty)
                .Replace("http://", "https://");

            var ageRating = DeriveAgeRating(info.MaturityRating, info.Categories);

            return new ExternalBookMetadata
            {
                CoverUrl = coverUrl,
                Description = info.Description,
                Author = info.Authors?.Length > 0 ? info.Authors[0] : null,
                AgeRating = ageRating,
                ProviderName = ProviderName
            };
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 429)
        {
            _logger.LogWarning("[GoogleBooks] Rate limited. Consider adding an API key in Metadata Settings.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GoogleBooks] Failed to fetch metadata for '{Title}'", title);
            return null;
        }
    }

    private static AgeRating? DeriveAgeRating(string? maturityRating, string[]? categories)
    {
        // Check Google's own maturity flag first
        if (!string.IsNullOrEmpty(maturityRating))
        {
            if (maturityRating.Equals("MATURE", StringComparison.OrdinalIgnoreCase))
                return AgeRating.Mature17Plus;
        }

        // Derive from categories (e.g. "Juvenile Fiction / Mystery", "Young Adult Fiction / Horror")
        if (categories?.Length > 0)
        {
            var combined = string.Join(" ", categories).ToLowerInvariant();
            return AgeRatingHelper.MapSubjectsToAgeRating(combined);
        }

        return null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GoogleBooksResult
    {
        [JsonPropertyName("items")]
        public GoogleBooksItem[]? Items { get; set; }
    }

    private sealed class GoogleBooksItem
    {
        [JsonPropertyName("volumeInfo")]
        public GoogleBooksVolumeInfo? VolumeInfo { get; set; }
    }

    private sealed class GoogleBooksVolumeInfo
    {
        [JsonPropertyName("authors")]
        public string[]? Authors { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("imageLinks")]
        public GoogleBooksImageLinks? ImageLinks { get; set; }

        [JsonPropertyName("maturityRating")]
        public string? MaturityRating { get; set; }

        [JsonPropertyName("categories")]
        public string[]? Categories { get; set; }
    }

    private sealed class GoogleBooksImageLinks
    {
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
    }
}
