using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // POST /webhook/game-event
    public class GameEventRequest
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("joueur")] public string Joueur;
        [JsonProperty("action")] public string Action;
    }

    public class GameEventResponse
    {
        [JsonProperty("status")] public string Status;
        [JsonProperty("message")] public string Message;
    }
}
