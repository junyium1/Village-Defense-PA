using UnityEngine;

namespace Game
{
    // marks the single point enemies path towards via NavMesh
    public class EnemyObjective : MonoBehaviour
    {
        public static Transform target;

        void Awake() => target = transform;
    }
}
