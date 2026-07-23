using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Détruit un ScriptableObject cloné à l'exécution quand le GameObject porteur est
    /// détruit, pour éviter d'accumuler des clones en mémoire (cf. UpgradeStatApplier).
    /// </summary>
    public class RuntimeDataCleaner : MonoBehaviour
    {
        private ScriptableObject _runtimeData;

        public void Track(ScriptableObject so) => _runtimeData = so;

        private void OnDestroy()
        {
            if (_runtimeData != null)
                Destroy(_runtimeData);
        }
    }
}
