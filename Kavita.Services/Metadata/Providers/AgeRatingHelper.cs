using Kavita.Models.Entities.Enums;

namespace Kavita.Services.Metadata.Providers;

/// <summary>
/// Maps genre/subject strings from various sources (ID3 tags, book metadata providers) to AgeRating values.
/// </summary>
public static class AgeRatingHelper
{
    /// <summary>
    /// Maps a combined lowercase subject/genre/category string to the most appropriate AgeRating.
    /// Returns null if no match found.
    /// </summary>
    public static AgeRating? MapSubjectsToAgeRating(string combined)
    {
        if (string.IsNullOrWhiteSpace(combined)) return null;

        // Most restrictive first
        if (combined.Contains("erotic") || combined.Contains("adults only") || combined.Contains("explicit"))
            return AgeRating.AdultsOnly;

        if (combined.Contains("mature") || combined.Contains("adult fiction") || combined.Contains("adult nonfiction"))
            return AgeRating.Mature17Plus;

        if (combined.Contains("young adult") || combined.Contains("ya fiction") || combined.Contains("teen"))
            return AgeRating.Teen;

        if (combined.Contains("juvenile") || combined.Contains("children") || combined.Contains("kids") ||
            combined.Contains("picture book") || combined.Contains("early reader"))
            return AgeRating.Everyone;

        return null;
    }

    /// <summary>
    /// Maps ID3 genre tags from an audio file to AgeRating.
    /// Returns null if no meaningful mapping found.
    /// </summary>
    public static AgeRating? MapGenresToAgeRating(string[]? genres)
    {
        if (genres == null || genres.Length == 0) return null;
        var combined = string.Join(" ", genres).ToLowerInvariant();
        return MapSubjectsToAgeRating(combined);
    }
}
