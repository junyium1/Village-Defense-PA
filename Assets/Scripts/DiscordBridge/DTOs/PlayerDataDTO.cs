using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // GET /api/player/{discord_id}
    public class PlayerDataResponse
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("mana")] public int Mana;
        [JsonProperty("notif_pref")] public string NotifPref; // "mp" | "fief" | "silence"

        // Buffs consommés non expirés, source de vérité SERVEUR (table effets_actifs).
        // Null si le serveur ne renvoie pas encore le champ (rétro-compatibilité).
        [JsonProperty("active_effects")] public List<ActiveEffectDto> ActiveEffects;
    }

    public class ActiveEffectDto
    {
        [JsonProperty("item_id")] public string ItemId;
        [JsonProperty("expires_at")] public double ExpiresAt; // epoch Unix, horloge SERVEUR

        // Préféré à ExpiresAt côté client : relatif, donc insensible au décalage
        // d'horloge entre la machine du joueur et le serveur.
        [JsonProperty("remaining_seconds")] public double RemainingSeconds;
    }
}
