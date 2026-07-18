using DiscordBridge.Data;
using TMPro;
using UnityEngine;

namespace DiscordBridge.UI
{
    // Vue pure : ne connaît que PlayerProfileData, jamais DiscordAPIBridge ni les requêtes HTTP.
    public class ManaHUD : MonoBehaviour
    {
        [SerializeField] PlayerProfileData profileData;
        [SerializeField] TextMeshProUGUI manaText;

        void Start()
        {
            if (profileData == null)
            {
                Debug.LogWarning("[DiscordBridge] ManaHUD : aucun PlayerProfileData assigné.");
                return;
            }

            profileData.OnProfileUpdated += HandleProfileUpdated;

            // Le profil peut déjà avoir été synchronisé avant que ce HUD n'existe (ex: changement
            // de scène) : on affiche l'état courant tout de suite plutôt que d'attendre le
            // prochain événement, qui pourrait ne jamais arriver si rien ne change entre-temps.
            if (profileData.IsLoaded)
                HandleProfileUpdated();
        }

        void OnDestroy()
        {
            if (profileData != null)
                profileData.OnProfileUpdated -= HandleProfileUpdated;
        }

        void HandleProfileUpdated()
        {
            if (manaText == null) return;
            manaText.text = profileData.Mana.ToString();
        }
    }
}
