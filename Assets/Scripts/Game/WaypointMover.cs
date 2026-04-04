using Game.Units;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Unit))]
    public class WaypointMover : MonoBehaviour
    {
        Unit  _unit;
        int   _waypointIndex = 0;
        Transform _target;

        void Awake() => _unit = GetComponent<Unit>();

        void Start()
        {
            if (Waypoints.waypoints.Length == 0)
            {
                Debug.LogError("No waypoints found!");
                return;
            }
            _target = Waypoints.waypoints[0];
        }

        void Update()
        {
            if (_target == null) return;

            Vector3 dir = _target.position - transform.position;
            transform.Translate(
                dir.normalized * _unit.data.moveSpeed * Time.deltaTime,
                Space.World
            );

            if (Vector3.Distance(transform.position, _target.position) <= 0.2f)
                NextWaypoint();
        }

        void NextWaypoint()
        {
            if (_waypointIndex >= Waypoints.waypoints.Length - 1)
            {
                // Enemy reached the end — notify CombatManager
                CombatManager.Instance.OnEnemyReachedEnd();
                Destroy(gameObject);
                return;
            }
            _waypointIndex++;
            _target = Waypoints.waypoints[_waypointIndex];
        }
    }
}