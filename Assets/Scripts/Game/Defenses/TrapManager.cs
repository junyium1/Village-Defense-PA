using System.Collections.Generic;
using UnityEngine;
using Game.Units;

namespace Game.Defenses
{
    public class TrapManager : MonoBehaviour
    {
        public TrapData data;

        readonly HashSet<Unit> _tauntedEnemies = new HashSet<Unit>();
        bool _hasExploded = false;

        void Update()
        {
            if (_hasExploded) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, data.triggerRadius);
            foreach (Collider hit in hits)
            {
                Unit unit = hit.GetComponent<Unit>();
                if (unit == null || unit.GetFaction() != Faction.Enemy) continue;

                TauntEnemy(unit); // HashSet.Add — no-op if already taunted
            }

            _tauntedEnemies.RemoveWhere(u => u == null); // only prune dead/destroyed units

            if (_tauntedEnemies.Count >= data.enemyThreshold)
            {
                Explode();
            }
        }

        void TauntEnemy(Unit unit)
        {
            if (_tauntedEnemies.Contains(unit)) return;

            _tauntedEnemies.Add(unit);

            var mover = unit.GetComponent<EnemyNavMover>();
            if (mover != null) mover.SetDestinationOverride(transform);
        }

        void Explode()
        {
            _hasExploded = true;

            Collider[] hits = Physics.OverlapSphere(transform.position, data.explosionRadius);
            foreach (Collider hit in hits)
            {
                Unit unit = hit.GetComponent<Unit>();
                if (!unit|| unit.GetFaction() != Faction.Enemy) continue;

                if (unit.health)
                {
                    unit.health.TakeDamage(data.damage);
                }
            }

            print("Mimic explodes!!!!!!");
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            if (data == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.triggerRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.explosionRadius);
        }
    }
}