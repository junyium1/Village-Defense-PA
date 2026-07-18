using System;
using System.Threading;
using DiscordBridge.Networking;
using DiscordBridge.Session;
using UnityEngine;

namespace DiscordBridge.Controllers
{
    // Orchestration de la liaison de compte : la vue (LinkAccountScreen) ne fait que déclencher
    // SubmitCodeAsync et réagir à OnLinkSucceeded/OnLinkFailed, jamais d'appel direct au bridge.
    public class LinkAccountController : MonoBehaviour
    {
        [SerializeField] ProfileSyncController profileSyncController;

        public event Action OnLinkSucceeded;
        public event Action<string> OnLinkFailed;

        // État de liaison exposé à la vue (qui ne doit jamais toucher SessionStore directement).
        public bool IsLinked => SessionStore.IsLinked;

        public event Action OnUnlinked;

        public async Awaitable SubmitCodeAsync(int code, CancellationToken cancellationToken = default)
        {
            var result = await DiscordAPIBridge.Instance.LinkAccountAsync(code, cancellationToken);

            if (!result.Success)
            {
                OnLinkFailed?.Invoke(result.ErrorMessage);
                return;
            }

            // /api/link-account renvoie discord_id en nombre JSON (cf. LinkAccountResponse.DiscordId,
            // qui est un long) ; SessionStore attend une string pour ne pas perdre de précision
            // sur le snowflake une fois qu'il repasse par PlayerPrefs.
            SessionStore.SetLinkedAccount(result.Data.DiscordId.ToString());

            if (profileSyncController != null)
                await profileSyncController.SyncAsync(cancellationToken);
            else
                Debug.LogWarning("[DiscordBridge] LinkAccountController : aucun ProfileSyncController assigné, synchro initiale ignorée.");

            OnLinkSucceeded?.Invoke();
        }

        // Délie le compte côté client (le joueur reste en base côté serveur ; seule la session locale oublie).
        public void Unlink()
        {
            SessionStore.ClearLinkedAccount();
            OnUnlinked?.Invoke();
        }
    }
}
