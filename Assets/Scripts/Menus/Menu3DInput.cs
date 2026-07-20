using UnityEngine;
using UnityEngine.InputSystem;

namespace Menus
{
    /// <summary>
    /// Raycast central du menu 3D : survol (hover) + clic sur les planches.
    /// New Input System (Mouse.current) — les messages OnMouse* hérités ne marchent pas.
    /// Input gelé pendant une transition (<see cref="SignpostRotator.IsBusy"/>,
    /// <see cref="SignpostPushSwap.IsBusy"/>).
    /// </summary>
    public class Menu3DInput : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] float maxRayDistance = 10f;

        SignPlankHover _hovered;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (cam == null || Mouse.current == null) return;

            // Les pancartes sont déplacées par script pendant la pause (timeScale = 0).
            // Sans step physique et avec Physics.autoSyncTransforms = false, les colliders
            // restent à leur ancienne position -> le raycast raterait les planches (il
            // testerait un fantôme resté près de l'origine). On resynchronise avant le tir.
            Physics.SyncTransforms();

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            bool hasHit = Physics.Raycast(ray, out hit, maxRayDistance) && !SignpostRotator.IsBusy && !SignpostPushSwap.IsBusy;

            SignPlankHover hov = hasHit ? hit.collider.GetComponentInParent<SignPlankHover>() : null;
            if (hov != _hovered)
            {
                if (_hovered != null) _hovered.SetHovered(false);
                if (hov != null) hov.SetHovered(true);
                _hovered = hov;
            }

            if (hasHit && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var plank = hit.collider.GetComponentInParent<SignPlankBase>();
                if (plank != null) plank.OnClicked();
            }
        }
    }
}
