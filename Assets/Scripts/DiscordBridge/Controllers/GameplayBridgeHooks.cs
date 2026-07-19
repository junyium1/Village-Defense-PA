using DiscordBridge.Networking;
using DiscordBridge.Session;
using Game;
using UnityEngine;

namespace DiscordBridge.Controllers
{
    // Unique point de couture entre la boucle de gameplay (CombatManager) et le pont Discord.
    // S'abonne aux événements du CombatManager de la scène de jeu et les traduit en appels
    // réseau ; le gameplay ne référence jamais DiscordBridge, et inversement ce script ne
    // touche à aucune logique de jeu.
    public class GameplayBridgeHooks : MonoBehaviour
    {
        [SerializeField] WaveRewardController waveRewardController;

        [Header("Alerte Discord (webhook game-event)")]
        [Tooltip("Prévenir Discord quand la DERNIÈRE vague du niveau démarre (assaut final).")]
        [SerializeField] bool notifyDiscordOnFinalWave = true;
        [Tooltip("Prévenir Discord à CHAQUE vague (spam : à réserver aux tests).")]
        [SerializeField] bool notifyDiscordEveryWave = false;

        CombatManager _combat;

        void Start()
        {
            _combat = CombatManager.Instance;
            if (_combat == null)
            {
                Debug.LogWarning("[DiscordBridge] GameplayBridgeHooks : aucun CombatManager dans la scène, hooks inactifs.");
                return;
            }

            _combat.WaveStarted += HandleWaveStarted;
            _combat.WaveRewardReady += HandleWaveRewardReady;
        }

        void OnDestroy()
        {
            if (_combat == null) return;
            _combat.WaveStarted -= HandleWaveStarted;
            _combat.WaveRewardReady -= HandleWaveRewardReady;
        }

        void HandleWaveRewardReady(int waveIndex, int enemiesDefeated)
        {
            if (waveRewardController == null)
            {
                Debug.LogWarning("[DiscordBridge] GameplayBridgeHooks : aucun WaveRewardController assigné.");
                return;
            }

            waveRewardController.OnWaveCompleted(waveIndex, enemiesDefeated);
        }

        void HandleWaveStarted(int waveIndex)
        {
            bool shouldNotify = notifyDiscordEveryWave || (notifyDiscordOnFinalWave && _combat.IsFinalWave);
            if (!shouldNotify) return;

            if (!SessionStore.IsLinked || DiscordAPIBridge.Instance == null) return;

            _ = NotifyAssaultAsync(waveIndex);
        }

        async Awaitable NotifyAssaultAsync(int waveIndex)
        {
            // "joueur" = nom de l'attaquant affiché dans l'embed Discord ; "action" = texte libre.
            var result = await DiscordAPIBridge.Instance.SendGameEventAsync(
                SessionStore.DiscordId,
                "Une horde ennemie",
                $"Vague {waveIndex} : le village est assailli !",
                destroyCancellationToken);

            if (result.Success)
                Debug.Log($"[DiscordBridge] Alerte Discord envoyée (vague {waveIndex}).");
            else
                Debug.LogWarning($"[DiscordBridge] Échec de l'alerte Discord ({result.StatusCode}) : {result.ErrorMessage}");
        }
    }
}
