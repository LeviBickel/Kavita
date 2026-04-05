using System.Threading;
using System.Threading.Tasks;
using Kavita.Models.DTOs.Metadata;

namespace Kavita.API.Services.Metadata;

/// <summary>
/// Implemented by each external book metadata provider (Open Library, Google Books, Hardcover)
/// </summary>
public interface IBookMetadataProvider
{
    /// <summary>
    /// Display name of the provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Fetch metadata for a book by title and optional author
    /// </summary>
    Task<ExternalBookMetadata?> FetchAsync(string title, string? author, string? apiKey, CancellationToken ct = default);
}
