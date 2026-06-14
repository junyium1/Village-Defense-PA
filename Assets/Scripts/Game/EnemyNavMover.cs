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

        void Update()
        {
            if (_agent.pathPending) return;

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                CombatManager.Instance.OnEnemyReachedEnd();
                Destroy(gameObject);
            }
        }
    }
}
