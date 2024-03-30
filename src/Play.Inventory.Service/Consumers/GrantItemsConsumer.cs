using System.Linq.Expressions;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using static Play.Inventory.Service.Dtos;

namespace Play.Inventory.Service.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    /// <summary>
    /// This is a reference to the MongoDatabase Collection (inventoryitems)
    /// </summary>
    private readonly IRepository<InventoryItem> inventoryItemsRepository;

    /// <summary>
    /// This is a reference to the MongoDatabase Collection (catalogitems)
    /// </summary>
    private readonly IRepository<CatalogItem> catalogItemsRepository;

    public GrantItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
    }

    // First, check if the item exist, if so, get the item and assign it to a user.
    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        var message = context.Message;

        var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

        if (item is null)
        {
            throw new UnknownItemException(message.CatalogItemId);
        }

        // Check if it exist, if it it's null create a new InventoryItem
        // Otherwise, increment the item and update it.
        Expression<Func<InventoryItem, bool>> filter =
            item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId;

        var inventoryItem = await inventoryItemsRepository.GetAsync(filter);

        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem()
            {
                CatalogItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow,
            };

            // attach the MessageId from the message header to check for duplicate messages
            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            // Avoid duplicate messages
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity += message.Quantity;

            // attach the MessageId from the message header to check for duplicate messages
            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await inventoryItemsRepository.UpdateAsync(inventoryItem);
        };

        // Publish messages for Trading Service
        var itemsGrantedTasks = context.Publish(new InventoryItemsGranted(message.CorrelationId));
        var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));

        await Task.WhenAll(inventoryUpdatedTask, itemsGrantedTasks);
    }
}
