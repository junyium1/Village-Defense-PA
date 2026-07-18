using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // POST /api/add-mana — le client ne choisit jamais le montant, seulement wave/enemies_killed,
    // signés en HMAC (voir DiscordAPIBridge.BuildManaSignature). Le serveur calcule la récompense.
    public class AddManaRequest
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("wave")] public int Wave;
        [JsonProperty("enemies_killed")] public int EnemiesKilled;
        [JsonProperty("nonce")] public string Nonce;
        [JsonProperty("timestamp")] public double Timestamp;
        [JsonProperty("signature")] public string Signature;
    }

    public class AddManaResponse
    {
        [JsonProperty("status")] public string Status;
        [JsonProperty("reward")] public int Reward;
        [JsonProperty("mana")] public int Mana;
        [JsonProperty("wave")] public int Wave;
    }
}
