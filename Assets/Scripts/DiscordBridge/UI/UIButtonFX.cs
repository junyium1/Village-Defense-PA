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
        [SerializeField] Transform targetTransform;

        Transform _target;
        Vector3 _baseScale;
        Coroutine _running;
        bool _hovered;
        bool _pressed;

        void Awake()
        {
            _target = targetTransform != null ? targetTransform : transform;
            _baseScale = _target.localScale;
        }

        void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            _running = null;
            if (_target != null) _target.localScale = _baseScale;
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
            Vector3 from = _target.localScale;
            for (float t = 0f; t < animDuration; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))
            {
                _target.localScale = Vector3.Lerp(from, target, t / animDuration);
                yield return null;
            }

            _target.localScale = target;
            _running = null;
        }
    }
}
