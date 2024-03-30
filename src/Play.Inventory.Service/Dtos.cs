namespace Play.Inventory.Service;

/// <summary>
/// This class maintains the list of Dtos that will be returned back to the client. 
/// </summary>
public class Dtos
{
	public record GrantItemsDto(Guid UserId, Guid CatalogItemId, int Quantity);
	public record InventoryItemDto(Guid CatalogItemId, string Name, string Description, int Quantity, DateTimeOffset AcquiredDate);
	public record CatalogItemDto(Guid Id, string Name, string Description);
}
