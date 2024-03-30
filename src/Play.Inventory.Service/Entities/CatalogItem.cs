using Play.Common;

namespace Play.Inventory.Service.Entities;

public class CatalogItem : IEntity
{
    /// <summary>
    /// Id of the IventoryItem.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the Catalog Item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the Catalog Item
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
