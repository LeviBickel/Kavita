namespace Kavita.Models.DTOs.Collection;
#nullable enable

public sealed record CreateSmartCollectionDto
{
    public required string Title { get; init; }
    public required int SmartFilterId { get; init; }
}
