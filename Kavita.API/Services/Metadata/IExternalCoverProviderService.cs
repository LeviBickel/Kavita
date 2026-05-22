using System.Threading;
using System.Threading.Tasks;
using Kavita.Models.DTOs.KavitaPlus.Metadata;
using Kavita.Models.DTOs.Metadata;

namespace Kavita.API.Services.Metadata;

/// <summary>
/// Orchestrates external cover art and metadata fetching across enabled providers
/// </summary>
public interface IExternalCoverProviderService
{
    /// <summary>
    /// Try each enabled provider in priority order and return the first result found.
    /// Returns null if no provider is enabled or none find a match.
    /// </summary>
    Task<ExternalBookMetadata?> FetchMetadataAsync(string title, string? author, MetadataSettingsDto settings, CancellationToken ct = default);

    /// <summary>
    /// Queries Google Books specifically for series membership information (seriesInfo).
    /// Only runs when Google Books is enabled; does not require a cover to be present.
    /// Returns null if Google Books is disabled, the book is not found, or the book has no series info.
    /// </summary>
    Task<ExternalBookMetadata?> FetchSeriesInfoAsync(string title, string? author, MetadataSettingsDto settings, CancellationToken ct = default);
}
