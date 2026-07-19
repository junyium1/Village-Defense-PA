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

        // Barème SERVEUR (ITEM_DURATIONS). 0 = effet instantané (ex: reinforce).
        [JsonProperty("duration_seconds")] public int DurationSeconds;

        // Epoch Unix de fin d'effet ; null si aucun effet à durée n'a été posé.
        [JsonProperty("expires_at")] public double? ExpiresAt;
    }
}
