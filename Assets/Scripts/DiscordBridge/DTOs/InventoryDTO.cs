using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscordBridge.DTOs
{
    // GET /api/inventory/{discord_id}
    public class InventoryItemDTO
    {
        [JsonProperty("item_id")] public string ItemId;
        [JsonProperty("date_achat")] public string DateAchat; // horodatage SQLite, format texte
        [JsonProperty("nom")] public string Nom;
        [JsonProperty("type")] public string Type; // "permanent" | "consumable"
    }

    public class InventoryResponse
    {
        [JsonProperty("discord_id")] public string DiscordId;
        [JsonProperty("inventory")] public List<InventoryItemDTO> Inventory;
    }
}
