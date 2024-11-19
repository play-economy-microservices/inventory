using System;
using System.Threading.Tasks;
using DnsClient.Internal;
using MassTransit;
using Microsoft.Extensions.Logging;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{
    public class GrantItemsConsumer : IConsumer<GrantItems>
    {
        private readonly IRepository<InventoryItem> inventoryItemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;
        private readonly ILogger<GrantItemsConsumer> logger;

        public GrantItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository, ILogger<GrantItemsConsumer> logger)
        {
            this.inventoryItemsRepository = inventoryItemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
            this.logger = logger;
        }

        // First, check if the item exist, if so, get the item and assign it to a user.
        public async Task Consume(ConsumeContext<GrantItems> context)
        {
            var message = context.Message;
            
            logger.LogInformation(
                "Received grant item request of {Quantity} of item {ItemId} from user {UserId} with CorrelationId {CorrelationId}", 
                message.Quantity, 
                message.CatalogItemId, 
                message.UserId,
                message.CorrelationId);

            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            // Check if it exist, if it's null create a new InventoryItem
            // Otherwise, increment the item and update it.
            var inventoryItem = await inventoryItemsRepository.GetAsync(
                item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = message.CatalogItemId,
                    UserId = message.UserId,
                    Quantity = message.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
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
            }

            // Publish messages for Trading Service
            var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
            var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity
            ));

            await Task.WhenAll(itemsGrantedTask, inventoryUpdatedTask);
        }
    }
}