using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Kavita.API.Services.Metadata;
using Kavita.Models.DTOs.Metadata;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Metadata.Providers;

public class OpenLibraryMetadataProvider : IBookMetadataProvider
{
    private readonly ILogger<OpenLibraryMetadataProvider> _logger;
    private const string SearchUrl = "https://openlibrary.org/search.json";
    private const string CoverUrl = "https://covers.openlibrary.org/b/id/{0}-L.jpg";

    public string ProviderName => "Open Library";

    public OpenLibraryMetadataProvider(ILogger<OpenLibraryMetadataProvider> logger)
    {
        _logger = logger;
    }

    public async Task<ExternalBookMetadata?> FetchAsync(string title, string? author, string? apiKey, CancellationToken ct = default)
    {
        try
        {
            var request = SearchUrl
                .SetQueryParam("title", title)
                .SetQueryParam("fields", "title,author_name,cover_i")
                .SetQueryParam("limit", 1);

            if (!string.IsNullOrWhiteSpace(author))
                request = request.SetQueryParam("author", author);

            var response = await request
                .WithTimeout(10)
                .GetStringAsync(cancellationToken: ct);

            var result = JsonSerializer.Deserialize<OpenLibrarySearchResult>(response, JsonOptions);
            var doc = result?.Docs?.Length > 0 ? result.Docs[0] : null;
            if (doc == null) return null;

            string? coverImageUrl = null;
            if (doc.CoverId.HasValue)
                coverImageUrl = string.Format(CoverUrl, doc.CoverId.Value);

            return new ExternalBookMetadata
            {
                CoverUrl = coverImageUrl,
                Author = doc.AuthorNames?.Length > 0 ? doc.AuthorNames[0] : null,
                ProviderName = ProviderName
            };
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OpenLibrary] Failed to fetch metadata for '{Title}'", title);
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class OpenLibrarySearchResult
    {
        [JsonPropertyName("docs")]
        public OpenLibraryDoc[]? Docs { get; set; }
    }

    private sealed class OpenLibraryDoc
    {
        [JsonPropertyName("cover_i")]
        public int? CoverId { get; set; }

        [JsonPropertyName("author_name")]
        public string[]? AuthorNames { get; set; }
    }
}
