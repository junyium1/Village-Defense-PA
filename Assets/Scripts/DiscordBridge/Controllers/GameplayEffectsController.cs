using System.Collections.Generic;
using DiscordBridge.Data;
using DiscordBridge.Session;
using Game;
using Game.Units;
using UnityEngine;
using UnityEngine.AI;

namespace DiscordBridge.Controllers
{
    // Applique EN JEU les effets des consommables Discord, lus depuis ActiveEffectsData
    // (lui-même aligné sur la table effets_actifs du serveur à chaque synchro) :
    //   freeze_5m  -> ennemis stoppés net (vitesse + attaques)
    //   shield_10m -> tout ce qui n'est pas ennemi devient invulnérable
    //   reinforce  -> réserve locale (PendingItemEffectsStore) : spawn d'alliés en début de partie
    // boost_30m n'a aucun effet client : le serveur double lui-même les gains de Mana.
    // Même rôle de "seam" que GameplayBridgeHooks : le gameplay pur ne référence jamais
    // DiscordBridge, c'est ce composant qui agit sur lui.
    public class GameplayEffectsController : MonoBehaviour
    {
        const string FreezeItemId = "freeze_5m";
        const string ShieldItemId = "shield_10m";
        const string ReinforceItemId = "reinforce";

        [SerializeField] ActiveEffectsData activeEffects;

        [Tooltip("Période du scan d'application (s). Les ennemis apparus pendant un gel sont attrapés au tick suivant.")]
        [SerializeField] float scanInterval = 0.25f;

        [Header("Renforts (reinforce)")]
        [SerializeField] GameObject reinforcementPrefab;
        [SerializeField] UnitData reinforcementData;
        [SerializeField] int reinforcementCount = 2;
        [SerializeField] float reinforcementSpawnRadius = 4f;

        readonly Dictionary<Unit, float> _frozenSpeeds = new();
        readonly List<Behaviour> _disabledAttacks = new();
        bool _reinforcementsDone;

        void OnEnable() => InvokeRepeating(nameof(Tick), 0.1f, scanInterval);

        void OnDisable()
        {
            CancelInvoke(nameof(Tick));
            UnfreezeAll();
        }

        void Tick()
        {
            if (activeEffects == null) return;

            ApplyFreeze(activeEffects.IsActive(FreezeItemId));
            ApplyShield(activeEffects.IsActive(ShieldItemId));
            TrySpawnReinforcements();
        }

        // --- GEL TEMPOREL ---

        void ApplyFreeze(bool frozen)
        {
            if (!frozen)
            {
                if (_frozenSpeeds.Count > 0) UnfreezeAll();
                return;
            }

            foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                if (unit.data == null || unit.data.faction != Faction.Enemy) continue;
                if (_frozenSpeeds.ContainsKey(unit)) continue;

                var agent = unit.GetComponent<NavMeshAgent>();
                _frozenSpeeds[unit] = agent != null ? agent.speed : 0f;
                if (agent != null)
                {
                    agent.speed = 0f;
                    agent.isStopped = true;
                }

                DisableIfPresent(unit.GetComponent<MeleeAttack>());
                DisableIfPresent(unit.GetComponent<RangedAttack>());
            }
        }

        void DisableIfPresent(Behaviour attack)
        {
            if (attack == null || !attack.enabled) return;
            attack.enabled = false;
            _disabledAttacks.Add(attack);
        }

        void UnfreezeAll()
        {
            foreach (KeyValuePair<Unit, float> entry in _frozenSpeeds)
            {
                Unit unit = entry.Key;
                if (unit == null) continue; // mort pendant le gel

                var agent = unit.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = entry.Value;
                    agent.isStopped = false;
                }
            }
            _frozenSpeeds.Clear();

            foreach (Behaviour attack in _disabledAttacks)
                if (attack != null) attack.enabled = true;
            _disabledAttacks.Clear();
        }

        // --- BOUCLIER ---

        // Posé/retiré à chaque tick : couvre les défenses placées pendant l'effet et
        // retire l'immunité naturellement à l'expiration.
        void ApplyShield(bool shielded)
        {
            foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                var unit = health.GetComponent<Unit>();
                bool isEnemy = unit != null && unit.data != null && unit.data.faction == Faction.Enemy;
                if (!isEnemy)
                    health.Invulnerable = shielded;
            }
        }

        // --- RENFORTS ---

        void TrySpawnReinforcements()
        {
            if (_reinforcementsDone) return;

            if (reinforcementPrefab == null || PendingItemEffectsStore.GetCount(ReinforceItemId) <= 0)
            {
                _reinforcementsDone = true;
                return;
            }

            // L'objectif sert de point d'ancrage : les renforts défendent le village.
            // Pas encore posé (ordre d'initialisation) -> on retente au tick suivant.
            Transform anchor = EnemyObjective.target;
            if (anchor == null) return;

            if (!PendingItemEffectsStore.TryConsume(ReinforceItemId))
            {
                _reinforcementsDone = true;
                return;
            }

            for (int i = 0; i < reinforcementCount; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, reinforcementCount);
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reinforcementSpawnRadius;
                GameObject go = Instantiate(reinforcementPrefab, anchor.position + offset, Quaternion.identity);

                var unit = go.GetComponent<Unit>();
                if (unit != null && reinforcementData != null)
                    unit.data = reinforcementData;
            }

            Debug.Log($"[DiscordBridge] Renforts déployés : {reinforcementCount}x {reinforcementPrefab.name}.");
            _reinforcementsDone = true;
        }
    }
}
