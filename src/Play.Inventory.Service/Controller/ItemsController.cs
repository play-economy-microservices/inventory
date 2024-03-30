using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using static Play.Inventory.Service.Dtos;

namespace Play.Inventory.Service.Controller;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
	private const string AdminRole = "Admin";

	/// <summary>
	/// This is a referenece to the MongoDatabase Collection
	/// </summary>
	private readonly IRepository<InventoryItem> inventoryItemsRepository;

	/// <summary>
	/// This is a referenece to the MongoDatabase Collection
	/// </summary>
	private readonly IRepository<CatalogItem> catalogItemsRepository;

	private readonly IPublishEndpoint publishEndpoint;


	/// <summary>
	/// Reference to talk to the Catalog Microservice (not used anymore)
	/// Migrated to RabbitMQ
	/// </summary>
	private readonly CatalogClient catalogClient;

	public ItemsController(
		IRepository<InventoryItem> inventoryItemsRepository,
		IRepository<CatalogItem> catalogItemsRepository,
		IPublishEndpoint publishEndpoint)
	{
		this.inventoryItemsRepository = inventoryItemsRepository;
		this.catalogItemsRepository = catalogItemsRepository;
		this.publishEndpoint = publishEndpoint;
	}

	[HttpGet]
	[Authorize]
	public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
	{
		if (userId == Guid.Empty)
		{
			return BadRequest();
		}

		// Retrieve the sub claim for this user (userId)
		var currentUserId = User.FindFirst(JwtRegisteredClaimNames.Sub).Value;

		// If the currentUser who is calling this api is not the matched user in db
		// and check if its role is not admin then they're not authorized to call this endpoint.
		if (Guid.Parse(currentUserId) != userId)
		{
			if (!User.IsInRole(AdminRole))
			{
				return Forbid();
			}
		}

		// Get Catalog Items from the Catalog Service 1 (not needed)
		//var catalogItems = await catalogClient.GetCatalogItemsAsync();

		Expression<Func<InventoryItem, bool>> filterFunc = item => item.UserId == userId;

		// The item UserId must be the same from the param userId within our Inventory Db collection
		// If so, grab the item only if it the catalogItem Id matches with inventory CatalogItemId.
		var inventoryItemEntities = await inventoryItemsRepository.GetAllAsync(filterFunc);
		var itemsIds = inventoryItemEntities.Select(item => item.CatalogItemId);

		// Get the catalog items again to obtain addtional information
		var catalogItemEntities = await catalogItemsRepository.GetAllAsync(item => itemsIds.Contains(item.Id));

		var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
		{
			var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
			return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
		});

		return Ok(inventoryItemDtos);
	}

	[HttpPost]
	[Authorize(Roles = AdminRole)]
	public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
	{
		// Check if it exist, if it it's null create a new InventoryItem
		// Otherwise, increment the item and update it.
		Expression<Func<InventoryItem, bool>> filter =
			item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId;

		var inventoryItem = await inventoryItemsRepository.GetAsync(filter);

		if (inventoryItem is null)
		{
			inventoryItem = new InventoryItem()
			{
				CatalogItemId = grantItemsDto.CatalogItemId,
				UserId = grantItemsDto.UserId,
				Quantity = grantItemsDto.Quantity,
				AcquiredDate = DateTimeOffset.UtcNow,
			};

			await inventoryItemsRepository.CreateAsync(inventoryItem);
		}
		else
		{
			inventoryItem.Quantity += grantItemsDto.Quantity;
			await inventoryItemsRepository.UpdateAsync(inventoryItem);
		}

		// for anyone who will grant items manually we should still publish this.
		await publishEndpoint.Publish(new InventoryItemUpdated(
			inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity
		));

		return Ok();
	}
}
