using System;
using DiscordBridge.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Une ligne d'inventaire : label "Nom xN [Type]" + bouton "Utiliser" (consommables
    // uniquement). Le clic remonte à l'écran via callback : la ligne ne connaît aucun
    // contrôleur ni réseau.
    public class InventoryRow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] Button useButton;

        InventoryEntry _entry;
        Action<InventoryEntry> _onUse;

        void Awake()
        {
            if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
        }

        void OnDestroy()
        {
            if (useButton != null) useButton.onClick.RemoveListener(OnUseClicked);
        }

        public void Bind(InventoryEntry entry, Action<InventoryEntry> onUse = null)
        {
            _entry = entry;
            _onUse = onUse;

            if (entry == null || entry.Definition == null) return;

            bool consumable = entry.Definition.Category == ItemCategory.Consumable;

            if (label != null)
            {
                string type = consumable ? "Consommable" : "Permanent";
                label.text = $"{entry.Definition.DisplayName}   x{entry.Count}   [{type}]";
            }

            if (useButton != null)
                useButton.gameObject.SetActive(consumable && onUse != null);
        }

        public void SetInteractable(bool interactable)
        {
            if (useButton != null) useButton.interactable = interactable;
        }

        void OnUseClicked()
        {
            if (_entry != null) _onUse?.Invoke(_entry);
        }
    }
}
