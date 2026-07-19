using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiscordBridge.Data
{
    // Miroir client des effets consommables ACTIFS. Source de vérité : le SERVEUR
    // (table effets_actifs, exposée dans /api/player.active_effects) — SyncFromServer
    // écrase l'état local à chaque synchro, ce qui fait survivre les buffs à un
    // redémarrage du jeu. Activate* ne sert qu'à démarrer l'effet immédiatement après
    // une consommation, sans attendre la synchro suivante. Le gameplay interroge
    // IsActive(id) / GetRemainingSeconds(id) (gel des ennemis, bouclier, etc.).
    [CreateAssetMenu(fileName = "ActiveEffectsData", menuName = "Discord Bridge/Runtime/Active Effects Data")]
    public class ActiveEffectsData : ScriptableObject
    {
        class ActiveEffect
        {
            public string ItemId;
            public double EndTime; // Time.realtimeSinceStartupAsDouble (insensible au timeScale/pause)
        }

        readonly List<ActiveEffect> _effects = new();

        public event Action OnEffectsChanged;

        void OnEnable() => ResetRuntimeState();

        // cf. PlayerProfileData.ResetRuntimeState : nécessaire même avec OnEnable pour couvrir
        // le cas "Reload Domain" désactivé entre deux lancements en Éditeur.
        public void ResetRuntimeState() => _effects.Clear();

        // Active (ou prolonge) un effet. duration <= 0 = effet instantané : rien à suivre.
        public void Activate(string itemId, int durationMinutes) =>
            ActivateForSeconds(itemId, durationMinutes * 60.0);

        public void ActivateForSeconds(string itemId, double durationSeconds)
        {
            if (string.IsNullOrEmpty(itemId) || durationSeconds <= 0.0) return;

            double endTime = Time.realtimeSinceStartupAsDouble + durationSeconds;
            ActiveEffect existing = _effects.Find(e => e.ItemId == itemId);
            if (existing != null)
                existing.EndTime = Math.Max(existing.EndTime, endTime); // ne raccourcit jamais
            else
                _effects.Add(new ActiveEffect { ItemId = itemId, EndTime = endTime });

            OnEffectsChanged?.Invoke();
        }

        // Remplace l'état local par celui du serveur (remaining_seconds, jamais expires_at :
        // le relatif est insensible au décalage d'horloge client/serveur). Notifie seulement
        // sur changement réel pour ne pas spammer l'UI à chaque polling de 30 s.
        public void SyncFromServer(IReadOnlyList<(string itemId, double remainingSeconds)> serverEffects)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            bool changed = false;

            // 1. Retire tout effet local que le serveur ne connaît plus (expiré côté serveur).
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                string id = _effects[i].ItemId;
                bool stillOnServer = serverEffects != null && Exists(serverEffects, id);
                if (!stillOnServer)
                {
                    _effects.RemoveAt(i);
                    changed = true;
                }
            }

            // 2. Aligne/ajoute chaque effet serveur (tolérance 2 s pour ignorer la latence réseau).
            if (serverEffects != null)
            {
                foreach ((string itemId, double remainingSeconds) in serverEffects)
                {
                    if (string.IsNullOrEmpty(itemId) || remainingSeconds <= 0.0) continue;

                    double endTime = now + remainingSeconds;
                    ActiveEffect existing = _effects.Find(e => e.ItemId == itemId);
                    if (existing == null)
                    {
                        _effects.Add(new ActiveEffect { ItemId = itemId, EndTime = endTime });
                        changed = true;
                    }
                    else if (Math.Abs(existing.EndTime - endTime) > 2.0)
                    {
                        existing.EndTime = endTime;
                        changed = true;
                    }
                }
            }

            if (changed)
                OnEffectsChanged?.Invoke();
        }

        static bool Exists(IReadOnlyList<(string itemId, double remainingSeconds)> list, string itemId)
        {
            foreach ((string id, double _) in list)
                if (id == itemId) return true;
            return false;
        }

        // Instantané des effets actifs (pour l'affichage UI), déjà purgé.
        public List<(string itemId, float remainingSeconds)> GetSnapshot()
        {
            Purge();
            double now = Time.realtimeSinceStartupAsDouble;
            var snapshot = new List<(string, float)>(_effects.Count);
            foreach (ActiveEffect effect in _effects)
                snapshot.Add((effect.ItemId, (float)(effect.EndTime - now)));
            return snapshot;
        }

        public bool IsActive(string itemId)
        {
            Purge();
            return _effects.Exists(e => e.ItemId == itemId);
        }

        public float GetRemainingSeconds(string itemId)
        {
            Purge();
            ActiveEffect effect = _effects.Find(e => e.ItemId == itemId);
            return effect == null ? 0f : (float)(effect.EndTime - Time.realtimeSinceStartupAsDouble);
        }

        // Retire les effets expirés ; notifie seulement si quelque chose a changé.
        void Purge()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            int removed = _effects.RemoveAll(e => e.EndTime <= now);
            if (removed > 0)
                OnEffectsChanged?.Invoke();
        }
    }
}
