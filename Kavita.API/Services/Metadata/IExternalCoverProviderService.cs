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
}
