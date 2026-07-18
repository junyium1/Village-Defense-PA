using System.Collections.Generic;
using UnityEngine;

namespace DiscordBridge.Data
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Discord Bridge/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] List<ItemDefinition> items = new();

        // Le catalogue tient sur une poignée d'objets (cf. ITEMS côté serveur) : une recherche
        // linéaire suffit largement, pas besoin d'un index en Dictionary.
        public ItemDefinition GetById(string itemId)
        {
            ItemDefinition found = items.Find(item => item.Id == itemId);
            if (found == null)
                Debug.LogWarning($"[DiscordBridge] Objet inconnu du catalogue local : \"{itemId}\". " +
                                  "Le serveur a peut-être un objet pas encore ajouté à l'ItemDatabase.");
            return found;
        }
    }
}
