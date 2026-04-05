using System.Collections.Generic;
using Kavita.Models.DTOs.Settings;
using Kavita.Models.Entities.Enums;
using Kavita.Models.Entities.MetadataMatching;

namespace Kavita.Models.DTOs.KavitaPlus.Metadata;


public sealed record MetadataSettingsDto: FieldMappingsDto
{
    /// <summary>
    /// If writing any sort of metadata from upstream (AniList, Hardcover) source is allowed
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Enable processing of metadata outside K+; e.g. disk and API
    /// </summary>
    public bool EnableExtendedMetadataProcessing { get; set; }

    /// <summary>
    /// Allow the Summary to be written
    /// </summary>
    public bool EnableSummary { get; set; }
    /// <summary>
    /// Allow Publication status to be derived and updated
    /// </summary>
    public bool EnablePublicationStatus { get; set; }
    /// <summary>
    /// Allow Relationships between series to be set
    /// </summary>
    public bool EnableRelationships { get; set; }
    /// <summary>
    /// Allow People to be created (including downloading images)
    /// </summary>
    public bool EnablePeople { get; set; }
    /// <summary>
    /// Allow Start date to be set within the Series
    /// </summary>
    public bool EnableStartDate { get; set; }
    /// <summary>
    /// Allow setting the Localized name
    /// </summary>
    public bool EnableLocalizedName { get; set; }
    /// <summary>
    /// Allow setting the cover image
    /// </summary>
    public bool EnableCoverImage { get; set; }

    #region Chapter Metadata
    /// <summary>
    /// Allow Summary to be set within Chapter/Issue
    /// </summary>
    public bool EnableChapterSummary { get; set; }
    /// <summary>
    /// Allow Release Date to be set within Chapter/Issue
    /// </summary>
    public bool EnableChapterReleaseDate { get; set; }
    /// <summary>
    /// Allow Title to be set within Chapter/Issue
    /// </summary>
        public bool EnableChapterTitle { get; set; }
    /// <summary>
    /// Allow Publisher to be set within Chapter/Issue
    /// </summary>
    public bool EnableChapterPublisher { get; set; }
    /// <summary>
    /// Allow setting the cover image for the Chapter/Issue
    /// </summary>
    public bool EnableChapterCoverImage { get; set; }
    #endregion

    // Need to handle the Genre/tags stuff
    public bool EnableGenres { get; set; } = true;
    public bool EnableTags { get; set; } = true;

    /// <summary>
    /// For Authors and Writers, how should names be stored (Exclusively applied for AniList). This does not affect Character names.
    /// </summary>
    public bool FirstLastPeopleNaming { get; set; }

    /// <summary>
    /// A list of overrides that will enable writing to locked fields
    /// </summary>
    public List<MetadataSettingField> Overrides { get; set; }

    /// <summary>
    /// Which Roles to allow metadata downloading for
    /// </summary>
    public List<PersonRole> PersonRoles { get; set; }

    #region External Cover Providers

    /// <summary>
    /// Fetch cover art from Open Library when no local cover is found
    /// </summary>
    public bool EnableOpenLibrary { get; set; }

    /// <summary>
    /// Fetch cover art from Google Books when no local cover is found
    /// </summary>
    public bool EnableGoogleBooks { get; set; }

    /// <summary>
    /// Optional Google Books API key for higher rate limits
    /// </summary>
    public string GoogleBooksApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Fetch cover art from Hardcover when no local cover is found
    /// </summary>
    public bool EnableHardcover { get; set; }

    /// <summary>
    /// Hardcover API key (required to use Hardcover). Masked in responses.
    /// </summary>
    public string HardcoverApiKey { get; set; } = string.Empty;

    #endregion

    /// <summary>
    /// Override list contains this field
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public bool HasOverride(MetadataSettingField field)
    {
        return Overrides.Contains(field);
    }

    /// <summary>
    /// If this Person role is allowed to be written
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    public bool IsPersonAllowed(PersonRole character)
    {
        return PersonRoles.Contains(character);
    }
}

/// <summary>
/// Decoupled from <see cref="MetadataSettingsDto"/> to allow reuse without requiring the full metadata settings in
/// <see cref="ImportFieldMappingsDto"/>
/// </summary>
public record FieldMappingsDto
{
    /// <summary>
    /// Do not allow any Genre/Tag in this list to be written to Kavita
    /// </summary>
    public List<string> Blacklist { get; set; }

    /// <summary>
    /// Only allow these Tags to be written to Kavita
    /// </summary>
    public List<string> Whitelist { get; set; }

    /// <summary>
    /// Any Genres or Tags that if present, will trigger an Age Rating Override. Highest rating will be prioritized for matching.
    /// </summary>
    public Dictionary<string, AgeRating> AgeRatingMappings { get; set; }

    /// <summary>
    /// A list of rules that allow mapping a genre/tag to another genre/tag
    /// </summary>
    public List<MetadataFieldMappingDto> FieldMappings { get; set; }
}
