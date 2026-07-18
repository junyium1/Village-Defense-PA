using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // POST /api/consume-item
    public class ConsumeItemRequest
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("item_id")] public string ItemId;
    }

    public class ConsumeItemResponse
    {
        [JsonProperty("status")] public string Status;
        [JsonProperty("item_id")] public string ItemId;
        [JsonProperty("remaining")] public int Remaining;
    }
}
