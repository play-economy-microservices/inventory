using System;
using Play.Inventory.Service.Entities;
using static Play.Inventory.Service.Dtos;

namespace Play.Inventory.Service;

/// <summary>
/// Dtos to be sent back to the client.
/// </summary>
public static class Extensions
{
	public static InventoryItemDto AsDto(this InventoryItem item, string name, string description)
	{
		return new InventoryItemDto(item.CatalogItemId, name, description, item.Quantity, item.AcquiredDate);
	}
} 
