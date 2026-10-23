using System.Threading;
using System.Threading.Tasks;

namespace Kavita.API.Services;

/// <summary>
/// Keeps local Smart Collections (collections backed by a Smart Filter) in sync with whatever series
/// currently match that filter. Purely local - no external service or license required.
/// </summary>
public interface ICollectionFilterSyncService
{
    /// <summary>
    /// Synchronize every Smart Collection
    /// </summary>
    Task SyncAll(CancellationToken ct = default);

    /// <summary>
    /// Synchronize a single Smart Collection
    /// </summary>
    Task SyncCollection(int collectionId, CancellationToken ct = default);
}
