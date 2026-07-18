using DiscordBridge.Data;
using TMPro;
using UnityEngine;

namespace DiscordBridge.UI
{
    // Une ligne d'inventaire (gray-box) : un seul label "Nom  xN  [Type]".
    // La passe deco pourra separer en colonnes / ajouter l'icone.
    public class InventoryRow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;

        public void Bind(InventoryEntry entry)
        {
            if (label == null || entry == null || entry.Definition == null) return;

            string type = entry.Definition.Category == ItemCategory.Permanent ? "Permanent" : "Consommable";
            label.text = $"{entry.Definition.DisplayName}   x{entry.Count}   [{type}]";
        }
    }
}
