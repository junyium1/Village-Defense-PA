using UnityEngine;

namespace Game.Defenses
{
    [RequireComponent(typeof(Health))]
    public class TurretManager : MonoBehaviour
    {
        public TurretData data;

        [SerializeField] Transform firePoint;
        [SerializeField] Transform rotatePart;

        Transform _target;
        float _fireCountdown;

        void Start()
        {
            GetComponent<Health>().maxHp = data.maxHp;
            InvokeRepeating(nameof(UpdateTarget), 0f, 0.5f);
        }

        void UpdateTarget()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float shortest = Mathf.Infinity;
            GameObject nearestEnemy = null;

            foreach (GameObject enemy in enemies)
            {
                float d = Vector3.Distance(transform.position, enemy.transform.position);
                if (d < shortest)
                {
                    shortest = d;
                    nearestEnemy = enemy;
                }
            }

            _target = nearestEnemy != null && shortest <= data.range
                ? nearestEnemy.transform
                : null;
        }

        void Update()
        {
            if (_target == null) return;

            // rotate toward target
            Vector3 dir = _target.position - transform.position;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            rotatePart.rotation = Quaternion.Euler(0f, lookRot.eulerAngles.y, 0f);

            // fire
            _fireCountdown -= Time.deltaTime;
            if (_fireCountdown <= 0f)
            {
                Shoot();
                _fireCountdown = 1f / data.fireRate;
            }
        }

        void Shoot()
        {
            GameObject go = Instantiate(data.bulletPrefab, firePoint.position, firePoint.rotation);
            Bullet bullet = go.GetComponent<Bullet>();
            if (bullet != null) bullet.Init(_target, data.damage, data.bulletSpeed);
        }

        void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.range);
        }
    }
}