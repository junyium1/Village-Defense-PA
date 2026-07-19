using System;
using System.Collections;
using UnityEngine;

namespace DiscordBridge.UI
{
    // Animation d'ouverture/fermeture d'un panneau : fondu (CanvasGroup) + léger zoom.
    // Purement cosmétique et autonome : les écrans l'appellent s'il est assigné, sinon
    // ils basculent SetActive directement. Temps non-scalé (fonctionne en pause).
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelAnimator : MonoBehaviour
    {
        [SerializeField] float openDuration = 0.22f;
        [SerializeField] float closeDuration = 0.15f;
        [SerializeField] float startScale = 0.92f;

        CanvasGroup _canvasGroup;
        Coroutine _running;

        void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

        // À appeler juste APRÈS SetActive(true).
        public void PlayOpen()
        {
            Restart(AnimateOpen());
        }

        // Fondu de sortie puis onDone (typiquement SetActive(false)).
        public void PlayClose(Action onDone)
        {
            // Objet déjà inactif (fermeture pendant une transition de scène) : pas de coroutine possible.
            if (!isActiveAndEnabled)
            {
                onDone?.Invoke();
                return;
            }

            Restart(AnimateClose(onDone));
        }

        void Restart(IEnumerator routine)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(routine);
        }

        IEnumerator AnimateOpen()
        {
            // dt plafonné : après un gros hitch (perte de focus éditeur, GC, chargement),
            // l'animation saute à la fin au lieu de rester figée sur un état intermédiaire.
            for (float t = 0f; t < openDuration; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
            {
                Apply(EaseOutCubic(t / openDuration));
                yield return null;
            }

            Apply(1f);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _running = null;
        }

        IEnumerator AnimateClose(Action onDone)
        {
            _canvasGroup.interactable = false; // plus de clics pendant la fermeture
            _canvasGroup.blocksRaycasts = false;

            for (float t = 0f; t < closeDuration; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
            {
                Apply(1f - EaseOutCubic(t / closeDuration));
                yield return null;
            }

            // État remis à "ouvert" pour que la PROCHAINE ouverture reparte propre même
            // si PlayOpen n'est pas rappelé.
            Apply(1f);
            _running = null;
            onDone?.Invoke();
        }

        // progress 0 -> invisible/rétréci, 1 -> visible/taille normale.
        void Apply(float progress)
        {
            _canvasGroup.alpha = progress;
            float scale = Mathf.LerpUnclamped(startScale, 1f, progress);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        static float EaseOutCubic(float x)
        {
            x = Mathf.Clamp01(x);
            float inv = 1f - x;
            return 1f - inv * inv * inv;
        }
    }
}
