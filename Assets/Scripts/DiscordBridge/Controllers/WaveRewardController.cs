using DiscordBridge.Networking;
using DiscordBridge.Session;
using UnityEngine;

namespace DiscordBridge.Controllers
{
    // Point d'entrée unique pour créditer du Mana à la fin d'une vague. Le gestionnaire de
    // vagues du jeu (CombatManager) appellera OnWaveCompleted une fois câblé ; pour l'instant
    // ce contrôleur reste autonome et découplé de la boucle de gameplay existante.
    public class WaveRewardController : MonoBehaviour
    {
        public void OnWaveCompleted(int waveIndex, int enemiesDefeated)
        {
            if (!SessionStore.IsLinked)
            {
                Debug.Log("[DiscordBridge] Aucun compte lié, récompense de vague ignorée.");
                return;
            }

            _ = RequestRewardAsync(waveIndex, enemiesDefeated);
        }

        async Awaitable RequestRewardAsync(int waveIndex, int enemiesDefeated)
        {
            var result = await DiscordAPIBridge.Instance.AddManaAsync(
                SessionStore.DiscordId, waveIndex, enemiesDefeated, destroyCancellationToken);

            if (result.Success)
            {
                Debug.Log($"[DiscordBridge] Vague {waveIndex} récompensée : +{result.Data.Reward} Mana (solde {result.Data.Mana}).");
            }
            else
            {
                Debug.LogWarning($"[DiscordBridge] Échec de la récompense de vague ({result.StatusCode}) : {result.ErrorMessage}");
            }
        }
    }
}
