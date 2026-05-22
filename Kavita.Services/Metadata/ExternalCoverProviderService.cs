using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kavita.API.Services.Metadata;
using Kavita.Models.DTOs.KavitaPlus.Metadata;
using Kavita.Models.DTOs.Metadata;
using Kavita.Services.Metadata.Providers;
using Microsoft.Extensions.Logging;

namespace Kavita.Services.Metadata;

public class ExternalCoverProviderService : IExternalCoverProviderService
{
    private readonly ILogger<ExternalCoverProviderService> _logger;
    private readonly OpenLibraryMetadataProvider _openLibrary;
    private readonly GoogleBooksMetadataProvider _googleBooks;
    private readonly HardcoverMetadataProvider _hardcover;

    public ExternalCoverProviderService(
        ILogger<ExternalCoverProviderService> logger,
        OpenLibraryMetadataProvider openLibrary,
        GoogleBooksMetadataProvider googleBooks,
        HardcoverMetadataProvider hardcover)
    {
        _logger = logger;
        _openLibrary = openLibrary;
        _googleBooks = googleBooks;
        _hardcover = hardcover;
    }

    public async Task<ExternalBookMetadata?> FetchMetadataAsync(string title, string? author, MetadataSettingsDto settings, CancellationToken ct = default)
    {
        var providers = BuildProviderList(settings);

        foreach (var (provider, apiKey) in providers)
        {
            if (ct.IsCancellationRequested) break;

            var result = await provider.FetchAsync(title, author, apiKey, ct);
            if (result?.CoverUrl != null)
            {
                _logger.LogDebug("[ExternalCoverProvider] Found cover for '{Title}' via {Provider}", title, provider.ProviderName);
                return result;
            }
        }

        _logger.LogDebug("[ExternalCoverProvider] No cover found for '{Title}' across all enabled providers", title);
        return null;
    }

    public async Task<ExternalBookMetadata?> FetchSeriesInfoAsync(string title, string? author, MetadataSettingsDto settings, CancellationToken ct = default)
    {
        if (!settings.EnableGoogleBooks) return null;

        var apiKey = string.IsNullOrWhiteSpace(settings.GoogleBooksApiKey) || settings.GoogleBooksApiKey == "***"
            ? null
            : settings.GoogleBooksApiKey;

        var result = await _googleBooks.FetchAsync(title, author, apiKey, ct);
        if (result?.SeriesName == null) return null;

        _logger.LogDebug("[ExternalCoverProvider] Found series info for '{Title}': series '{SeriesName}' #{SeriesIndex} via Google Books",
            title, result.SeriesName, result.SeriesIndex);
        return result;
    }

    private List<(IBookMetadataProvider Provider, string? ApiKey)> BuildProviderList(MetadataSettingsDto settings)
    {
        var list = new List<(IBookMetadataProvider, string?)>();

        if (settings.EnableOpenLibrary)
            list.Add((_openLibrary, null));

        if (settings.EnableGoogleBooks)
            list.Add((_googleBooks, string.IsNullOrWhiteSpace(settings.GoogleBooksApiKey) || settings.GoogleBooksApiKey == "***"
                ? null
                : settings.GoogleBooksApiKey));

        if (settings.EnableHardcover && !string.IsNullOrWhiteSpace(settings.HardcoverApiKey) && settings.HardcoverApiKey != "***")
            list.Add((_hardcover, settings.HardcoverApiKey));

        return list;
    }
}
