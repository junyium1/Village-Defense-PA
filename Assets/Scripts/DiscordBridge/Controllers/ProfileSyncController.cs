using System.Threading;
using DiscordBridge.Data;
using DiscordBridge.Networking;
using DiscordBridge.Session;
using UnityEngine;

namespace DiscordBridge.Controllers
{
    // Synchronise l'état serveur (mana, notif_pref, inventaire actif) vers les
    // ScriptableObjects runtime au démarrage. Ne fait rien si aucun compte n'est encore lié.
    public class ProfileSyncController : MonoBehaviour
    {
        [SerializeField] PlayerProfileData playerProfile;
        [SerializeField] InventoryData inventory;
        [SerializeField] ItemDatabase itemDatabase;
        [SerializeField] ActiveEffectsData activeEffects;

        [Tooltip("Re-synchronisation automatique toutes les N secondes (0 = désactivé). " +
                 "/api/player est la route de polling prévue côté serveur.")]
        [SerializeField] float pollIntervalSeconds = 0f;

        float _nextPollAt;
        bool _syncInFlight;

        void Awake()
        {
            // Awake de DiscordAPIBridge (qui fixe Instance) s'exécute avant le Start de cet
            // objet quel que soit l'ordre dans la scène : tous les Awake d'une frame de
            // chargement précèdent tous les Start. Le reset, lui, doit être immédiat pour ne
            // jamais laisser un vieil état visible avant la première synchro.
            playerProfile.ResetRuntimeState();
            inventory.ResetRuntimeState();
        }

        async void Start() => await SyncAsync(destroyCancellationToken);

        void Update()
        {
            if (pollIntervalSeconds <= 0f || _syncInFlight || !SessionStore.IsLinked) return;

            if (Time.unscaledTime >= _nextPollAt)
            {
                _nextPollAt = Time.unscaledTime + pollIntervalSeconds;
                _ = SyncAsync(destroyCancellationToken);
            }
        }

        public async Awaitable SyncAsync(CancellationToken cancellationToken = default)
        {
            if (!SessionStore.IsLinked)
            {
                Debug.Log("[DiscordBridge] Aucun compte lié, synchronisation du profil ignorée.");
                return;
            }

            if (_syncInFlight) return; // pas de synchros concurrentes (polling + bouton Rafraîchir)
            _syncInFlight = true;
            try
            {
                await SyncInternalAsync(cancellationToken);
            }
            finally
            {
                _syncInFlight = false;
            }
        }

        async Awaitable SyncInternalAsync(CancellationToken cancellationToken)
        {
            string discordId = SessionStore.DiscordId;

            var playerResult = await DiscordAPIBridge.Instance.GetPlayerDataAsync(discordId, cancellationToken);
            if (playerResult.Success)
            {
                playerProfile.Populate(playerResult.Data.Mana, playerResult.Data.NotifPref);
                ApplyServerEffects(playerResult.Data.ActiveEffects);
            }
            else
            {
                Debug.LogWarning($"[DiscordBridge] Échec de récupération du profil ({playerResult.StatusCode}) : {playerResult.ErrorMessage}");
            }

            var inventoryResult = await DiscordAPIBridge.Instance.GetInventoryAsync(discordId, cancellationToken);
            if (inventoryResult.Success)
            {
                inventory.Populate(inventoryResult.Data.Inventory, itemDatabase);
            }
            else
            {
                Debug.LogWarning($"[DiscordBridge] Échec de récupération de l'inventaire ({inventoryResult.StatusCode}) : {inventoryResult.ErrorMessage}");
            }
        }

        // Réaligne les buffs locaux sur la table effets_actifs du serveur — c'est ce qui
        // fait survivre un buff (gel, bouclier, boost) à un redémarrage du jeu.
        void ApplyServerEffects(System.Collections.Generic.List<DiscordBridge.DTOs.ActiveEffectDto> serverEffects)
        {
            if (activeEffects == null) return; // champ optionnel : ancien câblage de scène

            var mapped = new System.Collections.Generic.List<(string, double)>();
            if (serverEffects != null)
                foreach (DiscordBridge.DTOs.ActiveEffectDto dto in serverEffects)
                    mapped.Add((dto.ItemId, dto.RemainingSeconds));

            activeEffects.SyncFromServer(mapped);
        }
    }
}
