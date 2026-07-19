using UnityEngine;

namespace DiscordBridge.Data
{
    public enum ItemCategory
    {
        Permanent,
        Consumable
    }

    // Donnée statique d'un objet de la boutique Discord. Un asset par objet, catalogué dans
    // ItemDatabase. L'Id DOIT correspondre exactement à une clé du dictionnaire ITEMS côté
    // serveur (main.py) : c'est la clé de jointure entre les deux systèmes.
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Discord Bridge/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Doit correspondre exactement à une clé du dictionnaire ITEMS côté serveur (ex: \"shield_10m\").")]
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField, TextArea] public string Description { get; private set; }
        [field: SerializeField] public ItemCategory Category { get; private set; }

        [Tooltip("Durée de l'effet en minutes une fois consommé (0 = effet instantané ou objet permanent).")]
        [field: SerializeField] public int DurationMinutes { get; private set; }
    }
}
