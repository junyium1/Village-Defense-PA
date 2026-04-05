using UnityEngine;
using Game.Units;

namespace Game.Defenses
{
    public class PortalController : MonoBehaviour
    {
        public PortalData data;

        int _enemiesAbsorbed = 0;

        void Update()
        {
            // despawns when max capacity is reached
            if (_enemiesAbsorbed >= data.capacity)
            {
                Destroy(gameObject);
                return;
            }

            // eats enemies when they enter the trigger radius
            Collider[] hits = Physics.OverlapSphere(transform.position, data.triggerRadius);
            foreach (Collider hit in hits)
            {
                Unit unit = hit.GetComponent<Unit>();
                if (unit == null || unit.GetFaction() != Faction.Enemy) continue;

                AbsorbEnemy(unit);
            }
        }

        void AbsorbEnemy(Unit unit)
        {
            _enemiesAbsorbed++;
            print($"om nom nom nom... {unit.data.unitName}");
            Destroy(unit.gameObject);
        }

        // show radius inside of scene
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, data != null ? data.triggerRadius : 1f);
        }
    }
}