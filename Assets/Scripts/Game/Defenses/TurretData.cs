using UnityEngine;

namespace Game.Defenses
{
    // TODO implement status effect cf StatusEffectManager.cs
    public enum StatusEffect
    {
        None,
        Burn,
        Slow
    }

    [CreateAssetMenu(menuName = "Defense/Turret")]
    public class TurretData : Defenses.DefenseData
    {
        public float range;
        public float fireRate;
        public float damage;
        public float bulletSpeed;
        public StatusEffect statusEffect;
        public float statusDuration;
        public GameObject bulletPrefab;
    }
}