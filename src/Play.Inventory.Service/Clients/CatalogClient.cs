
using System;
using static Play.Inventory.Service.Dtos;

namespace Play.Inventory.Service.Clients;

/// <summary>
/// Class that handles invoking the REST Api from the Catalog Microservice.
/// </summary>
public class CatalogClient
{
	private readonly HttpClient httpClient;

	public CatalogClient(HttpClient httpClient)
	{
		this.httpClient = httpClient;
	}

	public async Task<IReadOnlyCollection<CatalogItemDto>> GetCatalogItemsAsync()
	{
		var items = await httpClient.GetFromJsonAsync<IReadOnlyCollection<CatalogItemDto>>("/items");
        return items;
	}
}
