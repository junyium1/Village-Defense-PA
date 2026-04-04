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
        [HideInInspector] public float currentHp;

        public event System.Action<Unit> OnDeath;

        void Start() => currentHp = data.maxHp;

        public void TakeDamage(float amount)
        {
            currentHp -= amount;
            if (currentHp <= 0) Die();
        }

        void Die()
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }
}