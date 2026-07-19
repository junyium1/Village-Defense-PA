using UnityEngine;

namespace Game.Units
{
    public enum Faction { Enemy, Ally }

    public class Unit : MonoBehaviour
    {
        public UnitData data;
        public Health health;

        void Awake() => health = GetComponent<Health>();

        public Faction GetFaction() => data.faction;
    }
}