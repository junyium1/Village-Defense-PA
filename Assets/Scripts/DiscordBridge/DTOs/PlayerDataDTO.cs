using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // GET /api/player/{discord_id}
    public class PlayerDataResponse
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("mana")] public int Mana;
        [JsonProperty("notif_pref")] public string NotifPref; // "mp" | "fief" | "silence"
    }
}
