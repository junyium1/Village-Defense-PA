using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // POST /api/link-account
    public class LinkAccountRequest
    {
        [JsonProperty("code")] public int Code;
    }

    public class LinkAccountResponse
    {
        [JsonProperty("status")] public string Status;

        // Le serveur sérialise ce champ en NOMBRE JSON ici (entier Python brut), contrairement à
        // /api/player et /api/inventory qui le renvoient en STRING (paramètre d'URL). Un snowflake
        // Discord tient dans un long (< 2^63) donc pas de perte de précision, mais ne copiez pas ce
        // type `long` sur les autres DTO : ailleurs c'est bien `string`.
        [JsonProperty("discord_id")] public long DiscordId;
    }
}
