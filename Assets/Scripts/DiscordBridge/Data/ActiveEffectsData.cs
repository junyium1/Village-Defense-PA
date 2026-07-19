using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiscordBridge.Data
{
    // Instancié au runtime : suit les effets consommables ACTIFS côté client (le serveur ne
    // suit que la possession, pas le temps restant). Alimenté par ConsumeItemController ;
    // le gameplay interroge IsActive(id) / GetRemainingSeconds(id) pour appliquer les effets
    // (gel des ennemis, bouclier de tour, etc.).
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
        public void Activate(string itemId, int durationMinutes)
        {
            if (string.IsNullOrEmpty(itemId) || durationMinutes <= 0) return;

            double endTime = Time.realtimeSinceStartupAsDouble + durationMinutes * 60.0;
            ActiveEffect existing = _effects.Find(e => e.ItemId == itemId);
            if (existing != null)
                existing.EndTime = Math.Max(existing.EndTime, endTime); // ne raccourcit jamais
            else
                _effects.Add(new ActiveEffect { ItemId = itemId, EndTime = endTime });

            OnEffectsChanged?.Invoke();
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
