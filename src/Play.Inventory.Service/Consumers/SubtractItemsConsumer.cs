using System.Linq.Expressions;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using static Play.Inventory.Service.Dtos;

namespace Play.Inventory.Service.Consumers;

public class SubtractItemsConsumer : IConsumer<SubtractItems>
{
    /// <summary>
    /// This is a referenece to the MongoDatabase Collection
    /// </summary>
    private readonly IRepository<InventoryItem> inventoryItemsRepository;

    /// <summary>
    /// This is a referenece to the MongoDatabase Collection
    /// </summary>
    private readonly IRepository<CatalogItem> catalogItemsRepository;

    public SubtractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
    }

    // First, check if the item exists in the db, if so, grab the item and subtract the quantity that was requested initially.
    public async Task Consume(ConsumeContext<SubtractItems> context)
    {
        var message = context.Message;

        var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

        if (item is null)
        {
            throw new UnknownItemException(message.CatalogItemId);
        }

        // Check the item exists and subtract the quantity that was requested
        Expression<Func<InventoryItem, bool>> filter =
            item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId;

        var inventoryItem = await inventoryItemsRepository.GetAsync(filter);

        if (inventoryItem is not null)
        {
            // Avoid duplicate messages
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
                return;
            }

            // Undo the quantity that was requested
            inventoryItem.Quantity -= message.Quantity;

            // attach the MessageId from the message header to check for duplicate messages
            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await inventoryItemsRepository.UpdateAsync(inventoryItem);

            // Publish messages for Trading Service
            await context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));
        }

        await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
    }
}
