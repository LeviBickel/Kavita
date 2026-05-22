using Kavita.Models.Entities.Enums;

namespace Kavita.Models.DTOs.Metadata;

/// <summary>
/// Metadata returned from an external book metadata provider
/// </summary>
public record ExternalBookMetadata
{
    /// <summary>
    /// Direct URL to a cover image
    /// </summary>
    public string? CoverUrl { get; init; }

    /// <summary>
    /// Book description / summary
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Primary author name
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Age rating derived from provider maturity/subject data. Null means unknown.
    /// </summary>
    public AgeRating? AgeRating { get; init; }

    /// <summary>
    /// Name of the provider that returned this result
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Series name from the provider (e.g. Google Books seriesInfo.displayName).
    /// Null when the provider did not return series membership data.
    /// </summary>
    public string? SeriesName { get; init; }

    /// <summary>
    /// The book's position within the series as a raw string (e.g. "1", "2", "1.5").
    /// Null when the provider did not return a position.
    /// </summary>
    public string? SeriesIndex { get; init; }
}
