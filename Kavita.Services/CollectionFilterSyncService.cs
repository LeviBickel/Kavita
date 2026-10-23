using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kavita.API.Database;
using Kavita.API.Repositories;
using Kavita.API.Services;
using Kavita.API.Services.SignalR;
using Kavita.Models.DTOs.Filtering.v2;
using Kavita.Models.DTOs.Filtering.v2.Requests;
using Kavita.Models.DTOs.SignalR;
using Kavita.Models.Entities;
using Kavita.Services.Helpers.SmartFilter;
using Microsoft.Extensions.Logging;

namespace Kavita.Services;

/// <summary>
/// Keeps local Smart Collections (collections backed by a Smart Filter) in sync with whatever series
/// currently match that filter. Purely local - no external service or license required.
/// </summary>
public class CollectionFilterSyncService(
    IUnitOfWork unitOfWork,
    ILogger<CollectionFilterSyncService> logger,
    IEventHub eventHub)
    : ICollectionFilterSyncService
{
    public async Task SyncAll(CancellationToken ct = default)
    {
        var ids = await unitOfWork.CollectionTagRepository.GetSmartCollectionIdsAsync(ct);
        logger.LogInformation("Found {Count} Smart Collections to synchronize", ids.Count);

        foreach (var id in ids)
        {
            try
            {
                await SyncCollection(id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error syncing Smart Collection {CollectionId}, continuing", id);
            }
        }

        logger.LogInformation("Smart Collection synchronization complete");
    }

    public async Task SyncCollection(int collectionId, CancellationToken ct = default)
    {
        var collection = await unitOfWork.CollectionTagRepository.GetCollectionAsync(collectionId,
            CollectionIncludes.Series | CollectionIncludes.SmartFilter, ct);

        if (collection?.SmartFilterId == null || collection.SmartFilter == null)
        {
            logger.LogInformation("Requested to sync Smart Collection {CollectionId} but it isn't filter-backed", collectionId);
            return;
        }

        IFilterDto decoded;
        try
        {
            decoded = SmartFilterHelper.Decode(collection.SmartFilter.Filter);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to decode Smart Filter for Smart Collection {CollectionName}", collection.Title);
            return;
        }

        if (decoded is not SeriesFilterV2Dto seriesFilter)
        {
            logger.LogWarning("Smart Collection {CollectionName}'s filter is not a Series filter, skipping sync", collection.Title);
            return;
        }

        var matched = await unitOfWork.SeriesRepository.GetSeriesForFilterAsync(collection.AppUserId, seriesFilter, ct);
        var matchedIds = matched.Select(s => s.Id).ToHashSet();

        collection.Items ??= new List<Series>();
        foreach (var stale in collection.Items.Where(s => !matchedIds.Contains(s.Id)).ToList())
        {
            collection.Items.Remove(stale);
        }

        var existingIds = collection.Items.Select(s => s.Id).ToHashSet();
        foreach (var series in matched.Where(series => !existingIds.Contains(series.Id)))
        {
            collection.Items.Add(series);
        }

        collection.LastSyncUtc = DateTime.UtcNow;
        unitOfWork.CollectionTagRepository.Update(collection);

        try
        {
            await unitOfWork.CommitAsync(ct);
            await unitOfWork.CollectionTagRepository.UpdateCollectionAgeRating(collection, ct);

            await eventHub.SendMessageAsync(MessageFactory.CollectionUpdated,
                MessageFactory.CollectionUpdatedEvent(collection.Id), false, ct);

            logger.LogInformation("Synced Smart Collection {CollectionName}: {Count} series", collection.Title, matchedIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an error saving Smart Collection {CollectionName}", collection.Title);
        }
    }
}
