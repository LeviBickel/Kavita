using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Flurl.Http;
using Kavita.API.Database;
using Kavita.API.Services;
using Kavita.API.Services.Helpers;
using Kavita.API.Services.Metadata;
using Kavita.API.Services.SignalR;
using Kavita.Common.Extensions;
using Kavita.Common.Helpers;
using Kavita.Models.DTOs.Metadata;
using Kavita.Models.DTOs.Settings;
using Kavita.Models.DTOs.SignalR;
using Kavita.Models.Entities;
using Kavita.Models.Entities.Enums;
using Kavita.Models.Entities.Interfaces;
using Kavita.Services.Comparators;
using Kavita.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace Kavita.Services;

/// <summary>
/// Handles everything around Cover/ColorScape management
/// </summary>
public class MetadataService(
    IUnitOfWork unitOfWork,
    ILogger<MetadataService> logger,
    IEventHub eventHub,
    ICacheHelper cacheHelper,
    IReadingItemService readingItemService,
    IDirectoryService directoryService,
    IImageService imageService,
    IExternalCoverProviderService externalCoverProviderService)
    : IMetadataService
{
    public const string Name = "MetadataService";
    private readonly IList<SignalRMessage> _updateEvents = new List<SignalRMessage>();

    /// <summary>
    /// Updates the metadata for a Chapter
    /// </summary>
    /// <param name="chapter"></param>
    /// <param name="forceUpdate">Force updating cover image even if underlying file has not been modified or chapter already has a cover image</param>
    /// <param name="encodeFormat">Convert image to Encoding Format when extracting the cover</param>
    /// <param name="forceColorScape">Force colorscape gen</param>
    private bool UpdateChapterCoverImage(Chapter? chapter, bool forceUpdate, EncodeFormat encodeFormat, CoverImageSize coverImageSize, bool forceColorScape = false)
    {
        if (chapter == null) return false;

        var firstFile = chapter.Files.MinBy(x => x.Chapter);
        if (firstFile == null) return false;

        if (!cacheHelper.ShouldUpdateCoverImage(
                directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, chapter.CoverImage),
                firstFile, chapter.Created, forceUpdate, chapter.CoverImageLocked))
        {
            if (NeedsColorSpace(chapter, forceColorScape))
            {
                imageService.UpdateColorScape(chapter);
                unitOfWork.ChapterRepository.Update(chapter);
                _updateEvents.Add(MessageFactory.CoverUpdateEvent(chapter.Id, MessageFactoryEntityTypes.Chapter));
            }

            return false;
        }


        logger.LogDebug("[MetadataService] Generating cover image for {File}", firstFile.FilePath);

        chapter.CoverImage = readingItemService.GetCoverImage(firstFile.FilePath,
            ImageService.GetChapterFormat(chapter.Id, chapter.VolumeId), firstFile.Format, encodeFormat, coverImageSize);

        imageService.UpdateColorScape(chapter);

        unitOfWork.ChapterRepository.Update(chapter);

        _updateEvents.Add(MessageFactory.CoverUpdateEvent(chapter.Id, MessageFactoryEntityTypes.Chapter));
        return true;
    }

    private void UpdateChapterLastModified(Chapter chapter, bool forceUpdate)
    {
        var firstFile = chapter.Files.MinBy(x => x.Chapter);
        if (firstFile == null || cacheHelper.IsFileUnmodifiedSinceCreationOrLastScan(chapter, forceUpdate, firstFile)) return;

        firstFile.UpdateLastModified();
    }

    private static bool NeedsColorSpace(IHasCoverImage? entity, bool force)
    {
        if (entity == null) return false;
        if (force) return true;

        return !string.IsNullOrEmpty(entity.CoverImage) &&
               (string.IsNullOrEmpty(entity.PrimaryColor) || string.IsNullOrEmpty(entity.SecondaryColor));
    }



    /// <summary>
    /// Updates the cover image for a Volume
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="forceUpdate">Force updating cover image even if underlying file has not been modified or chapter already has a cover image</param>
    /// <param name="forceColorScape">Force updating colorscape</param>
    private bool UpdateVolumeCoverImage(Volume? volume, bool forceUpdate, bool forceColorScape = false)
    {
        // We need to check if Volume coverImage matches first chapters if forceUpdate is false
        if (volume == null) return false;

        if (!cacheHelper.ShouldUpdateCoverImage(
                directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, volume.CoverImage),
                null, volume.Created, forceUpdate))
        {
            if (NeedsColorSpace(volume, forceColorScape))
            {
                imageService.UpdateColorScape(volume);
                unitOfWork.VolumeRepository.Update(volume);
                _updateEvents.Add(MessageFactory.CoverUpdateEvent(volume.Id, MessageFactoryEntityTypes.Volume));
            }
            return false;
        }

        if (!volume.CoverImageLocked)
        {
            // For cover selection, chapters need to try for issue 1 first, then fallback to first sort order
            volume.Chapters ??= new List<Chapter>();

            var firstChapter = volume.Chapters.FirstOrDefault(x => x.MinNumber.Is(1f));
            if (firstChapter == null)
            {
                firstChapter = volume.Chapters.MinBy(x => x.SortOrder, ChapterSortComparerDefaultFirst.Default);
                if (firstChapter == null) return false;
            }

            volume.CoverImage = firstChapter.CoverImage;
        }
        imageService.UpdateColorScape(volume);

        _updateEvents.Add(MessageFactory.CoverUpdateEvent(volume.Id, MessageFactoryEntityTypes.Volume));

        return true;
    }

    /// <summary>
    /// Updates cover image for Series
    /// </summary>
    /// <param name="series"></param>
    /// <param name="forceUpdate">Force updating cover image even if underlying file has not been modified or chapter already has a cover image</param>
    private void UpdateSeriesCoverImage(Series? series, bool forceUpdate, bool forceColorScape = false)
    {
        if (series == null) return;

        if (!cacheHelper.ShouldUpdateCoverImage(
                directoryService.FileSystem.Path.Join(directoryService.CoverImageDirectory, series.CoverImage),
                null, series.Created, forceUpdate, series.CoverImageLocked))
        {
            // Check if we don't have a primary/seconary color
            if (NeedsColorSpace(series, forceColorScape))
            {
                imageService.UpdateColorScape(series);
                _updateEvents.Add(MessageFactory.CoverUpdateEvent(series.Id, MessageFactoryEntityTypes.Series));
            }

            return;
        }

        series.Volumes ??= [];
        series.CoverImage = series.GetCoverImage();
        if (series.CoverImage == null)
        {
            logger.LogDebug("[SeriesCoverImageBug] Setting Series Cover Image to null: {SeriesId}", series.Id);
        }

        imageService.UpdateColorScape(series);

        _updateEvents.Add(MessageFactory.CoverUpdateEvent(series.Id, MessageFactoryEntityTypes.Series));
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="series"></param>
    /// <param name="forceUpdate"></param>
    /// <param name="encodeFormat"></param>
    private async Task ProcessSeriesCoverGen(Series series, bool forceUpdate, EncodeFormat encodeFormat, CoverImageSize coverImageSize, bool forceColorScape = false)
    {
        logger.LogDebug("[MetadataService] Processing cover image generation for series: {SeriesName}", series.OriginalName);
        try
        {
            var totalVolumes = series.Volumes.Count;
            var volumeIndex = 0;
            var firstVolumeUpdated = false;
            foreach (var volume in series.Volumes)
            {
                var firstChapterUpdated = false; // This only needs to be FirstChapter updated
                var index = 0;
                foreach (var chapter in volume.Chapters)
                {
                    var chapterUpdated = UpdateChapterCoverImage(chapter, forceUpdate, encodeFormat, coverImageSize, forceColorScape);
                    // If cover was update, either the file has changed or first scan, and we should force a metadata update
                    UpdateChapterLastModified(chapter, forceUpdate || chapterUpdated);
                    if (index == 0 && chapterUpdated)
                    {
                        firstChapterUpdated = true;
                    }

                    index++;
                }

                // Propagate within this volume: if any chapter has a cover, share it with those missing one.
                // This handles audiobooks where only some tracks have embedded art.
                var volumeFirstCover = volume.Chapters
                    .OrderBy(c => c.SortOrder)
                    .FirstOrDefault(c => !string.IsNullOrEmpty(c.CoverImage));
                if (volumeFirstCover != null)
                {
                    foreach (var chapter in volume.Chapters)
                    {
                        if (string.IsNullOrEmpty(chapter.CoverImage))
                        {
                            chapter.CoverImage = volumeFirstCover.CoverImage;
                            chapter.PrimaryColor = volumeFirstCover.PrimaryColor;
                            chapter.SecondaryColor = volumeFirstCover.SecondaryColor;
                            unitOfWork.ChapterRepository.Update(chapter);
                            _updateEvents.Add(MessageFactory.CoverUpdateEvent(chapter.Id, MessageFactoryEntityTypes.Chapter));
                            logger.LogDebug("[MetadataService] Propagated cover from Ch {SourceId} to Ch {TargetId} in volume {VolumeId}",
                                volumeFirstCover.Id, chapter.Id, volume.Id);
                        }
                    }
                }

                var volumeUpdated = UpdateVolumeCoverImage(volume, firstChapterUpdated || forceUpdate, forceColorScape);
                if (volumeIndex == 0 && volumeUpdated)
                {
                    firstVolumeUpdated = true;
                }

                await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
                    MessageFactory.CoverUpdateProgressEvent(series.LibraryId, volumeIndex / (float) totalVolumes, ProgressEventType.Started, series.Name));

                volumeIndex++;
            }

            UpdateSeriesCoverImage(series, firstVolumeUpdated || forceUpdate, forceColorScape);

            await TryFetchExternalCoverAsync(series, encodeFormat, coverImageSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MetadataService] There was an exception during cover generation for {SeriesName} ", series.Name);
        }
    }


    /// <summary>
    /// Attempts to enrich series metadata and fetch a cover image from enabled external providers.
    /// Series name enrichment: when a book's series name was derived from its file title (no EPUB series tag),
    /// Google Books is queried for the real series name and the Series entity is updated in-place.
    /// Cover fetch: only runs when the series has no cover. The Google Books result from the series
    /// enrichment step is reused when available, avoiding a second API call.
    /// Only runs for Book and Audiobook libraries.
    /// </summary>
    private async Task TryFetchExternalCoverAsync(Series series, EncodeFormat encodeFormat, CoverImageSize coverImageSize, CancellationToken ct = default)
    {
        var library = series.Library;
        if (library is not { Type: LibraryType.Book or LibraryType.Audiobook }) return;

        var metadataSettings = await unitOfWork.SettingsRepository.GetMetadataSettingDto(ct);
        if (!metadataSettings.EnableOpenLibrary && !metadataSettings.EnableGoogleBooks && !metadataSettings.EnableHardcover) return;

        var author = series.Metadata?.People
            ?.FirstOrDefault(p => p.Role == PersonRole.Writer)?.Person?.Name;

        // Strip "Author - " prefix from series name if present (common audiobook naming pattern)
        var title = series.Name;
        if (!string.IsNullOrEmpty(author) && title.StartsWith(author + " - ", StringComparison.OrdinalIgnoreCase))
            title = title[(author.Length + 3)..];
        else if (title.Contains(" - "))
            title = title[(title.IndexOf(" - ", StringComparison.Ordinal) + 3)..];

        // Strip leading track/chapter number left by " - " stripping (e.g. "03 The Cardinal..." -> "The Cardinal...")
        title = System.Text.RegularExpressions.Regex.Replace(title.TrimStart(), @"^\d+\s+", string.Empty);

        // Strip audiobook disc/part suffixes (e.g. "Joyland (Disc 04)" -> "Joyland", "Title (Part 2)" -> "Title")
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*\(Disc\s+\d+(?:\s+of\s+\d+)?\)\s*$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*\(Part\s+\d+\)\s*$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

        // Strip trailing volume/chapter number (e.g. "Snowbound Mystery 06" -> "Snowbound Mystery")
        title = System.Text.RegularExpressions.Regex.Replace(title.TrimEnd(), @"\s+\d+$", string.Empty);

        // --- Series name enrichment ---
        // When no calibre:series / belongs-to-collection tag was set in the EPUB the series name
        // is the book's own title. Detect that case and ask Google Books for the real series name.
        ExternalBookMetadata? googleResult = null;
        if (IsSeriesNameDerivedFromTitle(series))
        {
            logger.LogInformation("[MetadataService] '{SeriesName}' looks title-derived (single Special volume). Querying Google Books for series info",
                series.Name);
            googleResult = await externalCoverProviderService.FetchSeriesInfoAsync(title, author, metadataSettings, ct);
            if (googleResult?.SeriesName != null)
            {
                TryEnrichSeriesName(series, googleResult.SeriesName);
            }
            else
            {
                logger.LogInformation("[MetadataService] Google Books returned no series info for '{Title}' (author: '{Author}')",
                    title, author ?? "unknown");
            }
        }
        else
        {
            logger.LogDebug("[MetadataService] '{SeriesName}' skipped series enrichment: volumes={VolumeCount}, firstVolMinNumber={MinNumber}",
                series.Name,
                series.Volumes?.Count ?? -1,
                series.Volumes?.Count > 0 ? series.Volumes[0].MinNumber.ToString("F1") : "n/a");
        }

        // --- Cover fetching ---
        // If the series already has a cover, just propagate it to any volumes/chapters missing one.
        if (!string.IsNullOrEmpty(series.CoverImage))
        {
            logger.LogDebug("[MetadataService] Series '{SeriesName}' has cover '{Cover}', propagating to missing volumes/chapters",
                series.Name, series.CoverImage);
            await PropagateCoverToMissingVolumesAsync(series);
            return;
        }

        logger.LogDebug("[MetadataService] Series '{SeriesName}' has no cover, attempting external fetch", series.Name);

        // Reuse the Google Books result when it already has a cover (avoids a second API call);
        // otherwise fall through to the full provider chain.
        var result = (googleResult?.CoverUrl != null ? googleResult : null)
            ?? await externalCoverProviderService.FetchMetadataAsync(title, author, metadataSettings, ct);
        if (result?.CoverUrl == null) return;

        try
        {
            var stream = await result.CoverUrl.WithTimeout(15).GetStreamAsync(cancellationToken: ct);

            var firstChapter = series.Volumes
                .OrderBy(v => v.MinNumber)
                .SelectMany(v => v.Chapters)
                .FirstOrDefault();

            if (firstChapter == null) return;

            var fileName = ImageService.GetChapterFormat(firstChapter.Id, firstChapter.VolumeId);
            firstChapter.CoverImage = imageService.WriteCoverThumbnail(stream, fileName, directoryService.CoverImageDirectory, encodeFormat, coverImageSize);
            imageService.UpdateColorScape(firstChapter);
            unitOfWork.ChapterRepository.Update(firstChapter);

            // Propagate to all volumes and all their chapters that have no cover
            foreach (var volume in series.Volumes)
            {
                if (string.IsNullOrEmpty(volume.CoverImage))
                {
                    volume.CoverImage = firstChapter.CoverImage;
                    volume.PrimaryColor = firstChapter.PrimaryColor;
                    volume.SecondaryColor = firstChapter.SecondaryColor;
                    unitOfWork.VolumeRepository.Update(volume);
                    _updateEvents.Add(MessageFactory.CoverUpdateEvent(volume.Id, MessageFactoryEntityTypes.Volume));
                }

                // Set cover on all chapters in this volume that have no cover
                foreach (var chap in volume.Chapters)
                {
                    if (string.IsNullOrEmpty(chap.CoverImage))
                    {
                        chap.CoverImage = firstChapter.CoverImage;
                        chap.PrimaryColor = firstChapter.PrimaryColor;
                        chap.SecondaryColor = firstChapter.SecondaryColor;
                        unitOfWork.ChapterRepository.Update(chap);
                        _updateEvents.Add(MessageFactory.CoverUpdateEvent(chap.Id, MessageFactoryEntityTypes.Chapter));
                    }
                }
            }

            series.CoverImage = firstChapter.CoverImage;
            series.PrimaryColor = firstChapter.PrimaryColor;
            series.SecondaryColor = firstChapter.SecondaryColor;

            // Apply age rating from provider if series is still Unknown
            if (result.AgeRating.HasValue && series.Metadata != null && !series.Metadata.AgeRatingLocked
                && series.Metadata.AgeRating == AgeRating.Unknown)
            {
                series.Metadata.AgeRating = result.AgeRating.Value;
                logger.LogDebug("[MetadataService] Applied age rating {Rating} from {Provider} for '{SeriesName}'",
                    result.AgeRating.Value, result.ProviderName, series.Name);
            }

            unitOfWork.SeriesRepository.Update(series);

            _updateEvents.Add(MessageFactory.CoverUpdateEvent(firstChapter.Id, MessageFactoryEntityTypes.Chapter));
            _updateEvents.Add(MessageFactory.CoverUpdateEvent(series.Id, MessageFactoryEntityTypes.Series));

            logger.LogInformation("[MetadataService] Fetched external cover for '{SeriesName}' via {Provider}", series.Name, result.ProviderName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MetadataService] Failed to download external cover for '{SeriesName}' from {Url}", series.Name, result.CoverUrl);
        }
    }

    /// <summary>
    /// Returns true when a book's series name was auto-derived from its file title rather than from
    /// explicit EPUB metadata (calibre:series / belongs-to-collection).
    /// A reliable indicator: the series has exactly one volume and that volume is a Special (MinNumber == 0),
    /// meaning no series index was parsed from the file. This is true for every standalone book that
    /// lacks calibre series tags.
    /// </summary>
    private static bool IsSeriesNameDerivedFromTitle(Series series)
    {
        if (series.Volumes is not { Count: 1 }) return false;
        return series.Volumes[0].MinNumber == 0f;
    }

    /// <summary>
    /// Updates the series name (and sort name when not locked) to the value returned by Google Books.
    /// Only updates when the new name is non-empty and actually different from the current name.
    /// </summary>
    private void TryEnrichSeriesName(Series series, string googleSeriesName)
    {
        var normalized = googleSeriesName.ToNormalized();
        if (string.IsNullOrEmpty(normalized) || normalized == series.NormalizedName) return;

        logger.LogInformation("[MetadataService] Enriching '{OldName}' → '{NewName}' based on Google Books series info",
            series.Name, googleSeriesName);

        series.Name = googleSeriesName;
        series.NormalizedName = normalized;
        if (!series.SortNameLocked)
            series.SortName = googleSeriesName;

        unitOfWork.SeriesRepository.Update(series);
    }

    private async Task PropagateCoverToMissingVolumesAsync(Series series)
    {
        var anyUpdated = false;
        var chaptersUpdated = 0;
        foreach (var volume in series.Volumes)
        {
            if (string.IsNullOrEmpty(volume.CoverImage))
            {
                volume.CoverImage = series.CoverImage;
                volume.PrimaryColor = series.PrimaryColor;
                volume.SecondaryColor = series.SecondaryColor;
                unitOfWork.VolumeRepository.Update(volume);
                _updateEvents.Add(MessageFactory.CoverUpdateEvent(volume.Id, MessageFactoryEntityTypes.Volume));
                anyUpdated = true;
            }

            // Propagate to all chapters in this volume that have no cover
            foreach (var chap in volume.Chapters)
            {
                if (string.IsNullOrEmpty(chap.CoverImage))
                {
                    chap.CoverImage = series.CoverImage;
                    chap.PrimaryColor = series.PrimaryColor;
                    chap.SecondaryColor = series.SecondaryColor;
                    unitOfWork.ChapterRepository.Update(chap);
                    _updateEvents.Add(MessageFactory.CoverUpdateEvent(chap.Id, MessageFactoryEntityTypes.Chapter));
                    anyUpdated = true;
                    chaptersUpdated++;
                }
            }
        }

        if (anyUpdated)
        {
            logger.LogDebug("[MetadataService] Propagating cover to {Count} chapters for '{SeriesName}', committing", chaptersUpdated, series.Name);
            await unitOfWork.CommitAsync();
            logger.LogDebug("[MetadataService] Propagated existing cover to missing volumes/chapters for '{SeriesName}'", series.Name);
        }
    }

    /// <summary>
    /// Refreshes Cover Images for a whole library
    /// </summary>
    /// <remarks>This can be heavy on memory first run</remarks>
    /// <param name="libraryId"></param>
    /// <param name="forceUpdate">Force updating cover image even if underlying file has not been modified or chapter already has a cover image</param>
    /// <param name="forceColorScape">Force updating colorscape</param>
    /// <param name="ct"></param>
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 60 * 60)]
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task GenerateCoversForLibrary(int libraryId, bool forceUpdate = false, bool forceColorScape = false,
        CancellationToken ct = default)
    {
        var library = await unitOfWork.LibraryRepository.GetLibraryForIdAsync(libraryId, ct: ct);
        if (library == null) return;
        logger.LogInformation("[MetadataService] Beginning cover generation refresh of {LibraryName}", library.Name);

        _updateEvents.Clear();

        var chunkInfo = await unitOfWork.SeriesRepository.GetChunkInfoAsync(library.Id, ct);
        var stopwatch = Stopwatch.StartNew();
        var totalTime = 0L;
        logger.LogInformation("[MetadataService] Refreshing Library {LibraryName} for cover generation. Total Items: {TotalSize}. Total Chunks: {TotalChunks} with {ChunkSize} size", library.Name, chunkInfo.TotalSize, chunkInfo.TotalChunks, chunkInfo.ChunkSize);

        await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
            MessageFactory.CoverUpdateProgressEvent(library.Id, 0F, ProgressEventType.Started, $"Starting {library.Name}"), ct: ct);

        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var encodeFormat = settings.EncodeMediaAs;
        var coverImageSize = settings.CoverImageSize;

        for (var chunk = 1; chunk <= chunkInfo.TotalChunks; chunk++)
        {
            if (chunkInfo.TotalChunks == 0) continue;
            totalTime += stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            logger.LogDebug("[MetadataService] Processing chunk {ChunkNumber} / {TotalChunks} with size {ChunkSize}. Series ({SeriesStart} - {SeriesEnd})",
                chunk, chunkInfo.TotalChunks, chunkInfo.ChunkSize, chunk * chunkInfo.ChunkSize, (chunk + 1) * chunkInfo.ChunkSize);

            var nonLibrarySeries = await unitOfWork.SeriesRepository.GetFullSeriesForLibraryIdAsync(library.Id,
                new UserParams()
                {
                    PageNumber = chunk,
                    PageSize = chunkInfo.ChunkSize
                }, ct);
            logger.LogDebug("[MetadataService] Fetched {SeriesCount} series for refresh", nonLibrarySeries.Count);

            var seriesIndex = 0;
            foreach (var series in nonLibrarySeries)
            {
                try
                {
                    // Library is not included in GetFullSeriesForLibraryIdAsync, assign it so
                    // TryFetchExternalCoverAsync can check the library type
                    series.Library ??= library;
                    await ProcessSeriesCoverGen(series, forceUpdate, encodeFormat, coverImageSize, forceColorScape);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[MetadataService] There was an exception during cover generation refresh for {SeriesName}", series.Name);
                }
                seriesIndex++;

                var index = (chunk - 1) * chunkInfo.ChunkSize + seriesIndex;
                var progress = Math.Max(0F, Math.Min(1F, index * 1F / chunkInfo.TotalSize));
                await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
                    MessageFactory.CoverUpdateProgressEvent(library.Id, progress, ProgressEventType.Updated, series.Name), ct: ct);
            }

            await unitOfWork.CommitAsync(ct);

            await FlushEvents();

            logger.LogInformation(
                "[MetadataService] Processed {SeriesStart} - {SeriesEnd} out of {TotalSeries} series in {ElapsedScanTime} milliseconds for {LibraryName}",
                chunk * chunkInfo.ChunkSize, (chunk * chunkInfo.ChunkSize) + nonLibrarySeries.Count, chunkInfo.TotalSize, stopwatch.ElapsedMilliseconds, library.Name);
        }

        await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
            MessageFactory.CoverUpdateProgressEvent(library.Id, 1F, ProgressEventType.Ended, $"Complete"), ct: ct);

        logger.LogInformation("[MetadataService] Updated covers for {SeriesNumber} series in library {LibraryName} in {ElapsedMilliseconds} milliseconds total", chunkInfo.TotalSize, library.Name, totalTime);
    }


    public async Task RemoveAbandonedMetadataKeys(CancellationToken ct = default)
    {
        await unitOfWork.TagRepository.RemoveAllTagNoLongerAssociated(ct);
        await unitOfWork.PersonRepository.RemoveAllPeopleNoLongerAssociated(ct);
        await unitOfWork.GenreRepository.RemoveAllGenreNoLongerAssociated(ct: ct);
        await unitOfWork.CollectionTagRepository.RemoveCollectionsWithoutSeries(ct);
        await unitOfWork.AppUserProgressRepository.CleanupAbandonedChapters(ct);

    }

    /// <summary>
    /// Refreshes Metadata for a Series. Will always force updates.
    /// </summary>
    /// <param name="serverSetting"></param>
    /// <param name="libraryId"></param>
    /// <param name="seriesId"></param>
    /// <param name="forceUpdate">Overrides any cache logic and forces execution</param>
    /// <param name="forceColorScape">Will ensure that the colorscape is regenerated</param>
    /// <param name="ct"></param>
    public async Task GenerateCoversForSeries(ServerSettingDto serverSetting, int libraryId, int seriesId,
        bool forceUpdate = true, bool forceColorScape = true, CancellationToken ct = default)
    {
        var series = await unitOfWork.SeriesRepository.GetFullSeriesForSeriesIdAsync(seriesId, ct);
        if (series == null)
        {
            logger.LogError("[MetadataService] Series {SeriesId} was not found on Library {LibraryId}", seriesId, libraryId);
            return;
        }

        var encodeFormat = serverSetting.EncodeMediaAs;
        var coverImageSize = serverSetting.CoverImageSize;

        await GenerateCoversForSeries(series, encodeFormat, coverImageSize, forceUpdate, forceColorScape, ct);
    }

    /// <summary>
    /// Generate Cover for a Series. This is used by Scan Loop and should not be invoked directly via User Interaction.
    /// </summary>
    /// <param name="series">A full Series, with metadata, chapters, etc</param>
    /// <param name="encodeFormat">When saving the file, what encoding should be used</param>
    /// <param name="coverImageSize"></param>
    /// <param name="forceUpdate"></param>
    /// <param name="forceColorScape">Forces just colorscape generation</param>
    /// <param name="ct"></param>
    public async Task GenerateCoversForSeries(Series series, EncodeFormat encodeFormat, CoverImageSize coverImageSize,
        bool forceUpdate = false, bool forceColorScape = true, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
            MessageFactory.CoverUpdateProgressEvent(series.LibraryId, 0F, ProgressEventType.Started, series.Name), ct: ct);

        await ProcessSeriesCoverGen(series, forceUpdate, encodeFormat, coverImageSize, forceColorScape);


        if (unitOfWork.HasChanges())
        {
            await unitOfWork.CommitAsync(ct);
            logger.LogInformation("[MetadataService] Updated covers for {SeriesName} in {ElapsedMilliseconds} milliseconds", series.Name, sw.ElapsedMilliseconds);
        }

        await eventHub.SendMessageAsync(MessageFactory.NotificationProgress,
            MessageFactory.CoverUpdateProgressEvent(series.LibraryId, 1F, ProgressEventType.Ended, series.Name), ct: ct);

        await eventHub.SendMessageAsync(MessageFactory.CoverUpdate, MessageFactory.CoverUpdateEvent(series.Id, MessageFactoryEntityTypes.Series), false, ct);
        await FlushEvents();
    }

    private async Task FlushEvents()
    {
        // Send all events out now that entities are saved
        logger.LogDebug("Dispatching {Count} update events", _updateEvents.Count);
        foreach (var updateEvent in _updateEvents)
        {
            await eventHub.SendMessageAsync(MessageFactory.CoverUpdate, updateEvent, false);
        }
        _updateEvents.Clear();
    }
}
