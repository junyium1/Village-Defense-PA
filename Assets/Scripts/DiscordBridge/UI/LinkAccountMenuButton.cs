using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Bouton du menu principal qui ouvre l'écran de liaison à la demande.
    // Reste une "vue" : il ne connaît que LinkAccountScreen (pas le réseau ni la session).
    public class LinkAccountMenuButton : MonoBehaviour
    {
        [SerializeField] Button openButton;
        [SerializeField] LinkAccountScreen linkAccountScreen;

        void Awake()
        {
            if (openButton != null)
                openButton.onClick.AddListener(OnOpenClicked);
            else
                Debug.LogWarning("[DiscordBridge] LinkAccountMenuButton : aucun bouton assigné.");
        }

        void OnDestroy()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(OnOpenClicked);
        }

        void OnOpenClicked()
        {
            if (linkAccountScreen != null)
                linkAccountScreen.Open();
            else
                Debug.LogWarning("[DiscordBridge] LinkAccountMenuButton : aucun LinkAccountScreen assigné.");
        }
    }
}
