using UnityEngine;

namespace Game.Units
{
    public enum Faction { Enemy, Ally }

    // dynamic ish creation, specify values in inspector
    [CreateAssetMenu(menuName = "Units/UnitData")]
    public class UnitData : ScriptableObject {
        public string unitName;
        public Faction faction;
        public float maxHp, damage, moveSpeed, attackRange;
    }
    
    public class Unit: MonoBehaviour {
        public UnitData data;
        public Health   health;
        
        void Awake() => health = GetComponent<Health>();
        
        public Faction GetFaction() => data.faction;
    }
}