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
    /// Name of the provider that returned this result
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;
}
