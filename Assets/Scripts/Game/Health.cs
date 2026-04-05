using UnityEngine;

namespace Game
{
    public class Health : MonoBehaviour
    {
        public float maxHp = 10f;
        public float CurrentHp { get; private set; }
        public bool IsDead { get; private set; }

        public event System.Action<Health> OnDeath;
        public event System.Action<float, float> OnDamaged; // current, max — for UI

        void Awake() => CurrentHp = maxHp;

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHp -= amount;
            OnDamaged?.Invoke(CurrentHp, maxHp);
            if (CurrentHp <= 0) Die();
        }

        public void Heal(float amount)
        {
            CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
            OnDamaged?.Invoke(CurrentHp, maxHp);
        }

        void Die()
        {
            IsDead = true;
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
    }
}