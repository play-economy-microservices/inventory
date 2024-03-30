using System.Runtime.Serialization;

namespace Play.Inventory.Service.Consumers;

[Serializable]
internal class UnknownItemException : Exception
{
    public UnknownItemException(Guid ItemId) : base($"Unknown item '{ItemId}'")
    {
        this.ItemId = ItemId;
    }

    public Guid ItemId { get; }
}