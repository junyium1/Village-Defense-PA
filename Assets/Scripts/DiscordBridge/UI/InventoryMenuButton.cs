using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Bouton du menu principal qui ouvre l'ecran d'inventaire a la demande.
    // Reste une "vue" : il ne connait que InventoryScreen (pas le reseau ni la session).
    public class InventoryMenuButton : MonoBehaviour
    {
        [SerializeField] Button openButton;
        [SerializeField] InventoryScreen inventoryScreen;

        void Awake()
        {
            if (openButton != null)
                openButton.onClick.AddListener(OnOpenClicked);
            else
                Debug.LogWarning("[DiscordBridge] InventoryMenuButton : aucun bouton assigne.");
        }

        void OnDestroy()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(OnOpenClicked);
        }

        void OnOpenClicked()
        {
            if (inventoryScreen != null)
                inventoryScreen.Open();
            else
                Debug.LogWarning("[DiscordBridge] InventoryMenuButton : aucun InventoryScreen assigne.");
        }
    }
}
