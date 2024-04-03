namespace Play.Inventory.Contracts;

using System;

/// <summary>
/// Event for granting items
/// </summary>
public record GrantItems(
    Guid UserId,
    Guid CatalogItemId,
    int Quantity,
    Guid CorrelationId
    );

/// <summary>
/// Event response for the Event GrantItems was successfully received.
/// </summary>
public record InventoryItemsGranted(Guid CorrelationId);


/// <summary>
/// Event for failed GrantItems
/// </summary>
public record SubtractItems(
    Guid UserId,
    Guid CatalogItemId,
    int Quantity,
    Guid CorrelationId
    );

/// <summary>
/// Event response for the Event SubtractItems was successfully received.
/// </summary>
public record InventoryItemsSubtracted(Guid CorrelationId);

/// <summary>
/// Event when the Inventory Item has been updated for a User.
/// </summary>
public record InventoryItemUpdated(Guid UserId, Guid CatalogItemId, int NewTotalQuantity);

