using UnityEngine;

namespace Game.Units
{
    [RequireComponent(typeof(Unit))]
    public class RangedAttack : MonoBehaviour
    {
        Unit  _unit;
        float _attackTimer;

        void Awake() => _unit = GetComponent<Unit>();

        void Update()
        {
            if (_unit.health.IsDead) return;

            _attackTimer += Time.deltaTime;
            if (_attackTimer < 1f / _unit.data.attackRate) return;

            Unit target = FindNearestEnemy();
            if (target == null) return;

            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > _unit.data.attackRange) return;

            ShootAt(target);
            _attackTimer = 0f;
        }

        void ShootAt(Unit target)
        {
            if (_unit.data.bulletPrefab == null) return;

            GameObject go     = Instantiate(_unit.data.bulletPrefab, transform.position, Quaternion.identity);
            Bullet     bullet = go.GetComponent<Bullet>();
            if (bullet != null) bullet.Init(target.transform, _unit.data.damage, _unit.data.bulletSpeed);
        }

        Unit FindNearestEnemy()
        {
            Unit  nearest  = null;
            float bestDist = Mathf.Infinity;

            foreach (Unit u in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                if (u.GetFaction() == _unit.GetFaction()) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestDist) { bestDist = d; nearest = u; }
            }
            return nearest;
        }

        void OnDrawGizmosSelected()
        {
            if (_unit == null) _unit = GetComponent<Unit>();
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _unit.data.attackRange);
        }
    }
}