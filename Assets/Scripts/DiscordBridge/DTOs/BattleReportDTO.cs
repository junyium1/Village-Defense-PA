using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // POST /api/battle-report
    public class BattleReportRequest
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("wave_reached")] public int WaveReached;
        [JsonProperty("kills")] public int Kills;
        [JsonProperty("victory")] public bool Victory;
        [JsonProperty("duration_seconds")] public int DurationSeconds;
    }

    public class BattleReportResponse
    {
        [JsonProperty("status")] public string Status;
        [JsonProperty("message")] public string Message;
    }
}
