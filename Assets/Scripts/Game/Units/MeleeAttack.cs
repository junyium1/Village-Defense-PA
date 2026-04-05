using UnityEngine;

namespace Game.Units
{
    [RequireComponent(typeof(Unit))]
    public class MeleeAttack : MonoBehaviour
    {
        Unit  _unit;
        float _attackTimer;

        void Awake() => _unit = GetComponent<Unit>();

        void Update()
        {
            if (_unit.health.IsDead) return;

            _attackTimer += Time.deltaTime;
            if (_attackTimer < 1f / _unit.data.attackRate) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _unit.data.attackRange);

            foreach (Collider hit in hits)
            {
                Unit target = hit.GetComponent<Unit>();
                if (target == null || target.GetFaction() == _unit.GetFaction()) continue;

                target.health.TakeDamage(_unit.data.damage);
                _attackTimer = 0f;
                break;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (_unit == null) _unit = GetComponent<Unit>();
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _unit.data.attackRange);
        }
    }
}