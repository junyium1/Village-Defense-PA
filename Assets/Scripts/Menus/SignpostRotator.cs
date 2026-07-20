using System.Collections;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Pirouette du signpost : quart de tour sortant (0°→90°, panneau de face → tranche),
    /// swap du contenu à la tranche (invisible), quart de tour entrant (−90°→0°).
    /// Caméra fixe, pivot commun aux 3 panneaux. dt plafonné 0.05.
    /// </summary>
    public class SignpostRotator : MonoBehaviour
    {
        [SerializeField] Transform flipPivot;
        [SerializeField] float duration = 0.5f;

        /// <summary>Vrai pendant une pirouette : l'input (hover + clic) est gelé.</summary>
        public static bool IsBusy { get; private set; }

        /// <summary>Lance la pirouette ; <paramref name="swapAtEdge"/> est appelé à la tranche (contenu invisible).</summary>
        public void Flip(System.Action swapAtEdge)
        {
            if (IsBusy)
            {
                if (swapAtEdge != null) swapAtEdge();
                return;
            }
            StartCoroutine(FlipRoutine(swapAtEdge));
        }

        IEnumerator FlipRoutine(System.Action swapAtEdge)
        {
            IsBusy = true;
            yield return Rotate(0f, 90f, duration * 0.5f);
            if (swapAtEdge != null) swapAtEdge();
            yield return Rotate(-90f, 0f, duration * 0.5f);
            if (flipPivot != null) flipPivot.localRotation = Quaternion.identity;
            IsBusy = false;
        }

        IEnumerator Rotate(float fromDeg, float toDeg, float dur)
        {
            if (flipPivot == null || dur <= 0f) yield break;
            float t = 0f;
            while (t < dur)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                float a = Mathf.Lerp(fromDeg, toDeg, Mathf.Clamp01(t / dur));
                flipPivot.localRotation = Quaternion.Euler(0f, a, 0f);
                yield return null;
            }
            flipPivot.localRotation = Quaternion.Euler(0f, toDeg, 0f);
        }
    }
}
