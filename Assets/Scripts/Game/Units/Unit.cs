using System.Collections.Generic;
using UnityEngine;

namespace Game.Units
{
    public enum Faction { Enemy, Ally }

    public class Unit : MonoBehaviour
    {
        /// <summary>Registre de toutes les unites actives (minimap, effets).
        /// Maintenu par OnEnable/OnDisable : les unites detruites en sortent.</summary>
        public static readonly List<Unit> All = new List<Unit>();

        public UnitData data;
        public Health health;

        void Awake() => health = GetComponent<Health>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Faction GetFaction() => data.faction;
    }
}
