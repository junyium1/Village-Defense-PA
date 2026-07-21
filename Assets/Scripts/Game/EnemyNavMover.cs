using Game.Units;
using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyNavMover : MonoBehaviour
    {
        Unit _unit;
        NavMeshAgent _agent;
        Transform _destinationOverride;

        void Awake()
        {
            _unit = GetComponent<Unit>();
            _agent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            _agent.speed = _unit.data.moveSpeed;

            if (EnemyObjective.target == null)
            {
                Debug.LogError("No EnemyObjective found!");
                return;
            }

            _agent.SetDestination(EnemyObjective.target.position);
        }

        public void SetDestinationOverride(Transform target)
        {
            _destinationOverride = target;
        }

        void Update()
        {
            bool overrideActive = _destinationOverride != null;

            if (overrideActive)
            {
                _agent.SetDestination(_destinationOverride.position);
            }
            else
            {
                if (EnemyObjective.target == null) return;
                _agent.SetDestination(EnemyObjective.target.position);
            }

            if (_agent.pathPending) return;

            if (_agent.remainingDistance <= _agent.stoppingDistance && !overrideActive)
            {
                CombatManager.Instance.OnEnemyReachedEnd();
                Destroy(gameObject);
            }
        }
    }
}