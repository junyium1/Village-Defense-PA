using System.Collections;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Applique les réglages persistés (<see cref="SettingsStore"/>) au lancement,
    /// avec un léger différé : le moteur audio applique le snapshot par défaut du
    /// mixer APRES les RuntimeInitializeOnLoadMethod (1er tick audio), ce qui
    /// écraserait un SetFloat immédiat. Une application à ~0,05 s puis ~0,3 s
    /// couvre ce décalage ; inaudible au démarrage.
    /// </summary>
    public class SettingsApplier : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("[SettingsApplier]");
            DontDestroyOnLoad(go);
            go.AddComponent<SettingsApplier>();
        }

        IEnumerator Start()
        {
            // La qualité n'est pas écrasée : applicable immédiatement.
            yield return new WaitForSecondsRealtime(0.05f);
            SettingsStore.ApplyAll();
            // Filet de sécurité si le snapshot du mixer est appliqué tardivement.
            yield return new WaitForSecondsRealtime(0.25f);
            SettingsStore.ApplyAll();
            Destroy(gameObject);
        }
    }
}
