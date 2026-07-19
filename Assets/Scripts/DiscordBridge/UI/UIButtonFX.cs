using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DiscordBridge.UI
{
    // Micro-animation de bouton : grossit légèrement au survol, se compresse au clic.
    // Purement cosmétique, aucun impact sur la logique du Button sous-jacent.
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] float hoverScale = 1.05f;
        [SerializeField] float pressedScale = 0.96f;
        [SerializeField] float animDuration = 0.08f;

        Vector3 _baseScale;
        Coroutine _running;
        bool _hovered;
        bool _pressed;

        void Awake() => _baseScale = transform.localScale;

        void OnDisable()
        {
            // Un objet désactivé ne reçoit plus PointerExit : on remet tout à zéro.
            _hovered = false;
            _pressed = false;
            _running = null;
            transform.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; AnimateToCurrent(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; AnimateToCurrent(); }
        public void OnPointerDown(PointerEventData eventData) { _pressed = true; AnimateToCurrent(); }
        public void OnPointerUp(PointerEventData eventData) { _pressed = false; AnimateToCurrent(); }

        void AnimateToCurrent()
        {
            if (!isActiveAndEnabled) return;

            float target = _pressed ? pressedScale : (_hovered ? hoverScale : 1f);
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(AnimateScale(_baseScale * target));
        }

        IEnumerator AnimateScale(Vector3 target)
        {
            Vector3 from = transform.localScale;
            for (float t = 0f; t < animDuration; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
            {
                transform.localScale = Vector3.Lerp(from, target, t / animDuration);
                yield return null;
            }

            transform.localScale = target;
            _running = null;
        }
    }
}
