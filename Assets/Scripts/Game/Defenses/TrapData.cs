using UnityEngine;

namespace Game.Defenses
{
    [CreateAssetMenu(menuName = "Defense/Trap")]
    public class TrapData : Defenses.DefenseData
    {
        public float triggerRadius;
        public int enemyThreshold;
        public float tauntDuration;
        public float damage;
        public float explosionRadius;
    }
}