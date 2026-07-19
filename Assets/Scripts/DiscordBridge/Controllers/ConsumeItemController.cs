using System;
using System.Threading;
using DiscordBridge.Data;
using DiscordBridge.Networking;
using DiscordBridge.Session;
using UnityEngine;

namespace DiscordBridge.Controllers
{
    // Orchestration de la consommation d'un objet : la vue (InventoryScreen) appelle
    // ConsumeAsync et réagit aux events, jamais d'appel direct au bridge.
    // Sur succès serveur : active l'effet client (ActiveEffectsData) puis re-synchronise
    // l'inventaire pour que l'UI reflète l'exemplaire retiré (FIFO côté serveur).
    public class ConsumeItemController : MonoBehaviour
    {
        [SerializeField] ProfileSyncController profileSyncController;
        [SerializeField] ActiveEffectsData activeEffects;
        [SerializeField] ItemDatabase itemDatabase;

        public event Action<string> OnConsumeSucceeded;      // itemId
        public event Action<string, string> OnConsumeFailed; // itemId, message d'erreur

        public async Awaitable ConsumeAsync(string itemId, CancellationToken cancellationToken = default)
        {
            if (!SessionStore.IsLinked)
            {
                OnConsumeFailed?.Invoke(itemId, "Aucun compte lié.");
                return;
            }

            if (DiscordAPIBridge.Instance == null)
            {
                OnConsumeFailed?.Invoke(itemId, "Connexion au serveur indisponible.");
                return;
            }

            var result = await DiscordAPIBridge.Instance.ConsumeItemAsync(SessionStore.DiscordId, itemId, cancellationToken);

            if (!result.Success)
            {
                OnConsumeFailed?.Invoke(itemId, result.ErrorMessage);
                return;
            }

            // L'effet démarre côté client au moment de la consommation validée par le serveur.
            if (activeEffects != null && itemDatabase != null)
            {
                ItemDefinition definition = itemDatabase.GetById(itemId);
                if (definition != null)
                    activeEffects.Activate(itemId, definition.DurationMinutes);
            }

            if (profileSyncController != null)
                await profileSyncController.SyncAsync(cancellationToken);

            OnConsumeSucceeded?.Invoke(itemId);
        }
    }
}
