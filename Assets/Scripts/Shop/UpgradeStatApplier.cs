using Game;
using Game.Defenses;
using Game.Units;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Applique les multiplicateurs d'upgrade (dégâts / PV / vitesse) sur une instance
    /// fraîchement posée, selon le niveau d'upgrade stocké côté Player.
    ///
    /// Pour ne JAMAIS muter l'asset partagé (les managers lisent data.* en direct, ce qui
    /// affecterait toutes les instances et persisterait sur disque en éditeur), chaque
    /// instance reçoit un CLONE de son ScriptableObject de stats. Un RuntimeDataCleaner
    /// détruit ce clone quand l'objet meurt (évite les fuites mémoire).
    /// </summary>
    public static class UpgradeStatApplier
    {
        public static void Apply(GameObject instance, ShopItemData item)
        {
            if (instance == null || item == null || ShopManager.Instance == null)
                return;

            float dmgMult   = ShopManager.Instance.GetDamageMultiplier(item);
            float hpMult    = ShopManager.Instance.GetHpMultiplier(item);
            float speedMult = ShopManager.Instance.GetSpeedMultiplier(item);

            // Aucun upgrade actif : rien à faire (évite un clone inutile).
            if (Mathf.Approximately(dmgMult, 1f) &&
                Mathf.Approximately(hpMult, 1f) &&
                Mathf.Approximately(speedMult, 1f))
                return;

            // --- Tourelle ---
            var turret = instance.GetComponent<TurretManager>();
            if (turret != null && turret.data != null)
            {
                TurretData clone = Object.Instantiate(turret.data);
                clone.damage *= dmgMult;
                clone.maxHp  *= hpMult;
                turret.data = clone;
                instance.AddComponent<RuntimeDataCleaner>().Track(clone);

                // Son Awake a déjà poussé l'ancien maxHp dans Health, et son Start le
                // relira sur le clone : on resynchronise maxHp + PV courants tout de suite.
                SetHealthMax(instance, clone.maxHp);
                return;
            }

            // --- Piège ---
            var trap = instance.GetComponent<TrapManager>();
            if (trap != null && trap.data != null)
            {
                TrapData clone = Object.Instantiate(trap.data);
                clone.damage *= dmgMult;
                clone.maxHp  *= hpMult;
                trap.data = clone;
                instance.AddComponent<RuntimeDataCleaner>().Track(clone);

                ScaleHealth(instance, hpMult); // Health optionnel sur le piège
                return;
            }

            // --- Unité / troupe posée ---
            var unit = instance.GetComponent<Unit>();
            if (unit != null && unit.data != null)
            {
                UnitData clone = Object.Instantiate(unit.data);
                clone.damage    *= dmgMult;
                clone.maxHp     *= hpMult;
                clone.moveSpeed *= speedMult; // lu par EnemyNavMover.Start (frame suivante)
                unit.data = clone;
                instance.AddComponent<RuntimeDataCleaner>().Track(clone);

                // L'unité ne recopie jamais UnitData.maxHp dans Health : on scale le Health
                // courant pour que le multiplicateur de PV ait un effet réel.
                ScaleHealth(instance, hpMult);
                return;
            }

            // --- Autres (murs, portail...) : seul le PV a du sens s'il y a un Health. ---
            ScaleHealth(instance, hpMult);
        }

        // Fixe une valeur de PV max précise puis resynchronise les PV courants.
        static void SetHealthMax(GameObject go, float newMax)
        {
            Health hp = go.GetComponent<Health>();
            if (hp == null) return;
            hp.maxHp = newMax;
            hp.Init();
        }

        // Multiplie les PV max courants (base = valeur du prefab/manager) puis resynchronise.
        static void ScaleHealth(GameObject go, float mult)
        {
            if (Mathf.Approximately(mult, 1f)) return;
            Health hp = go.GetComponent<Health>();
            if (hp == null) return;
            hp.maxHp *= mult;
            hp.Init();
        }
    }
}
