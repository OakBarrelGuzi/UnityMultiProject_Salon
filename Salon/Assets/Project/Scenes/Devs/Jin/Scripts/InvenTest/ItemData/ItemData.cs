public class ItemData
{
    public float itemCost { get; set; }

    public string itemName { get; set; }

    public itemType itemType { get; set; }
}

public enum itemType
{
    Anime,
    Emoji,
}