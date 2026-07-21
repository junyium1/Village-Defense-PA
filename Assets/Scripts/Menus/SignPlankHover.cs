using System.Collections;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Feedback de survol d'une planche 3D : agrandissement + inclinaison + glow blanc.
    /// Les meshes importés ayant leur pivot très loin de la géométrie, on met à l'échelle
    /// et on incline AUTOUR du centre visuel (bounds), pas du pivot. dt plafonné 0.05.
    /// Le glow éclaircit la planche vers le blanc via MaterialPropertyBlock (aucune fuite
    /// sur le matériau partagé, aucun mot-clé shader requis). Piloté par <see cref="Menu3DInput"/>.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SignPlankHover : MonoBehaviour
    {
        [SerializeField] float hoverScale = 1.05f;
        [SerializeField] float tiltDegrees = 6f;
        [SerializeField] float speed = 10f;
        [SerializeField] Color glowColor = Color.white;
        [SerializeField, Range(0f, 1f)] float glowStrength = 0.6f;
        [Tooltip("Temps non-scalé : OBLIGATOIRE en menu pause (timeScale = 0).\n" +
                 "Laisser faux dans le menu principal.")]
        [SerializeField] bool useUnscaledTime = false;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Vector3 _baseScale;
        Vector3 _basePos;
        Quaternion _baseRot;
        Vector3 _center;
        Renderer _rend;
        MaterialPropertyBlock _mpb;
        Color _baseColor;
        bool _hasBaseColor;
        Coroutine _co;
        float _k;
        bool _hovered;
        bool _init;

        // Base capturée paresseusement au 1er survol : à ce moment le pivot est au repos
        // (l'input hover est gelé pendant une pirouette), donc pose correcte même pour les
        // planches des sous-panneaux qui s'activent en pleine rotation.
        void EnsureBase()
        {
            if (_init) return;
            _baseScale = transform.localScale;
            _basePos = transform.position;
            _baseRot = transform.rotation;
            _rend = GetComponent<Renderer>();
            _center = _rend != null ? _rend.bounds.center : transform.position;
            if (_rend != null)
            {
                _mpb = new MaterialPropertyBlock();
                var m = _rend.sharedMaterial;
                if (m != null && m.HasProperty(BaseColorId)) { _baseColor = m.GetColor(BaseColorId); _hasBaseColor = true; }
                else if (m != null && m.HasProperty(ColorId)) { _baseColor = m.GetColor(ColorId); _hasBaseColor = true; }
            }
            _init = true;
        }

        public void SetHovered(bool on)
        {
            if (_hovered == on) return;
            _hovered = on;
            if (on) EnsureBase();
            if (_co != null) { StopCoroutine(_co); _co = null; }
            if (!gameObject.activeInHierarchy) return;
            _co = StartCoroutine(Anim(on ? 1f : 0f));
        }

        IEnumerator Anim(float target)
        {
            while (Mathf.Abs(_k - target) > 0.001f)
            {
                float dt = Mathf.Min(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime, 0.05f);
                _k = Mathf.MoveTowards(_k, target, dt * speed);
                Apply();
                yield return null;
            }
            _k = target;
            Apply();
        }

        void Apply()
        {
            if (!_init) return;

            float s = Mathf.Lerp(1f, hoverScale, _k);
            float ang = Mathf.Lerp(0f, tiltDegrees, _k);
            transform.localScale = _baseScale * s;
            transform.position = _center + (_basePos - _center) * s;
            transform.rotation = _baseRot;
            transform.RotateAround(_center, Vector3.right, ang);

            if (_rend != null && _mpb != null && _hasBaseColor)
            {
                _rend.GetPropertyBlock(_mpb);
                float g = _k * glowStrength;
                _mpb.SetColor(BaseColorId, Color.Lerp(_baseColor, glowColor, g));
                _mpb.SetColor(EmissionId, glowColor * g);
                _rend.SetPropertyBlock(_mpb);
            }
        }
    }
}
