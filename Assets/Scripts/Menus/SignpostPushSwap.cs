using System.Collections;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Transition Options ⇄ Menu principal façon « bousculade » (T-11).
    /// Aller : la pancarte enchaînée arrive en PENDULE (pivot = haut des chaînes,
    /// relevée vers la caméra), percute le signpost titre qui BASCULE EN ARRIÈRE
    /// (pivot = base du poteau, chute ease-in + rebond), puis la pancarte se
    /// stabilise à la verticale par oscillation amortie. Retour = miroir
    /// (pancarte remportée vers la caméra, titre qui se relève avec ressort).
    /// Pas de collisions réelles : simple chorégraphie temporisée.
    /// Auto-câblé par <see cref="Menu3DController"/> (aucune modif de scène requise) ;
    /// poser le composant à la main sur le GO Menu3D pour régler les paramètres.
    /// Convention d'angles (axe X monde) : négatif = vers la caméra (+z) pour le
    /// pendule / vers l'arrière (−z) pour la chute. dt plafonné 0.05.
    /// </summary>
    public class SignpostPushSwap : MonoBehaviour
    {
        [Header("Pendule (pancarte options)")]
        [Tooltip("Angle de départ du pendule, relevé vers la caméra (négatif).")]
        public float swingStartDeg = -55f;
        [Tooltip("Durée du lâché jusqu'au dépassement.")]
        public float swingDownDuration = 0.5f;
        [Tooltip("Dépassement arrière après le contact (positif).")]
        public float swingOvershootDeg = 8f;
        [Tooltip("Durée de l'oscillation amortie de stabilisation.")]
        public float swingSettleDuration = 0.6f;
        [Tooltip("Avance du « contact » avant la verticale : la chute du titre démarre\n" +
                 "quand la pancarte atteint cet angle (évite le chevauchement visuel).")]
        public float contactLeadDeg = 8f;

        [Header("Chute du titre (signpost)")]
        [Tooltip("Angle final de la chute en arrière (positif, appliqué en négatif).")]
        public float fallAngleDeg = 85f;
        public float fallDuration = 0.45f;
        [Tooltip("Amplitude du rebond au sol.")]
        public float fallBounceDeg = 6f;
        public float fallBounceDuration = 0.18f;

        [Header("Retour (miroir)")]
        [Tooltip("Durée de la pancarte remportée vers la caméra.")]
        public float swingOutDuration = 0.45f;
        [Tooltip("Durée du relèvement du titre (effet ressort).")]
        public float riseDuration = 0.5f;
        [Tooltip("Délai avant que le titre commence à se relever.")]
        public float riseDelay = 0.08f;

        /// <summary>Vrai pendant la chorégraphie : l'input (hover + clic) est gelé.</summary>
        public static bool IsBusy { get; private set; }

        Transform _signpost;    // Signpost_Root (tombe / se relève)
        Transform _options;     // ChainedSign_Root (pendule)

        Vector3 _swingPivot;    // haut des chaînes (monde)
        Vector3 _fallPivot;     // base du poteau (monde)

        // Poses « maison » (locales) capturées au câblage — restaurées exactement
        // en fin d'animation pour ne jamais cumuler de dérive flottante.
        Vector3 _homeSignPos;
        Quaternion _homeSignRot;
        Vector3 _homeOptPos;
        Quaternion _homeOptRot;

        float _angleOptions;    // angle courant appliqué à la pancarte
        float _angleSignpost;   // angle courant appliqué au titre (0 = debout, −fall = couché)
        bool _fallDone = true;
        bool _riseDone = true;

        /// <summary>Câble les racines des deux panneaux (appelé par Menu3DController à l'Awake).</summary>
        public void SetTargets(Transform signpostRoot, Transform optionsRoot)
        {
            _signpost = signpostRoot;
            _options = optionsRoot;
            if (_signpost != null) { _homeSignPos = _signpost.localPosition; _homeSignRot = _signpost.localRotation; }
            if (_options != null) { _homeOptPos = _options.localPosition; _homeOptRot = _options.localRotation; }
            ComputePivots();
        }

        /// <summary>Restaure la pose de repos exacte du signpost (garde-fou hors chorégraphie).</summary>
        public void SnapHomeSignpost()
        {
            if (_signpost == null) return;
            _signpost.localPosition = _homeSignPos;
            _signpost.localRotation = _homeSignRot;
            _angleSignpost = 0f;
        }

        void SnapHomeOptions()
        {
            if (_options == null) return;
            _options.localPosition = _homeOptPos;
            _options.localRotation = _homeOptRot;
            _angleOptions = 0f;
        }

        // Pivots calculés sur les bounds à la pose de repos (monde) :
        // pendule = haut des chaînes, chute = base du poteau.
        void ComputePivots()
        {
            if (_options != null)
            {
                Bounds b = GetWorldBounds(_options);
                _swingPivot = new Vector3(b.center.x, b.max.y, b.center.z);
            }
            if (_signpost != null)
            {
                Bounds b = GetWorldBounds(_signpost);
                _fallPivot = new Vector3(b.center.x, b.min.y, b.center.z);
            }
        }

        static Bounds GetWorldBounds(Transform root)
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return new Bounds(root.position, Vector3.zero);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        /// <summary>Aller vers les Options : pendule entrant + chute du titre. Gère lui-même les SetActive.</summary>
        public void SwingIn(System.Action onDone)
        {
            if (_signpost == null || _options == null || !isActiveAndEnabled)
            {
                if (onDone != null) onDone();
                return;
            }
            if (IsBusy)
            {
                // Même contrat que SignpostRotator.Flip : ne jamais bloquer la navigation.
                ApplySwingInEndState();
                if (onDone != null) onDone();
                return;
            }
            StartCoroutine(SwingInRoutine(onDone));
        }

        /// <summary>Retour au menu principal : pancarte remportée + titre relevé. Gère lui-même les SetActive.</summary>
        public void SwingOut(System.Action onDone)
        {
            if (_signpost == null || _options == null || !isActiveAndEnabled)
            {
                if (onDone != null) onDone();
                return;
            }
            if (IsBusy)
            {
                ApplySwingOutEndState();
                if (onDone != null) onDone();
                return;
            }
            StartCoroutine(SwingOutRoutine(onDone));
        }

        IEnumerator SwingInRoutine(System.Action onDone)
        {
            IsBusy = true;

            // Pancarte au départ du pendule, activée (le titre est encore debout).
            SnapHomeOptions();
            _options.gameObject.SetActive(true);
            ApplySwing(swingStartDeg);

            // Phase A1 : lâché — accélération jusqu'au contact (la chute du titre
            // démarre à contactLeadDeg avant la verticale).
            bool fallStarted = false;
            float half1 = swingDownDuration * 0.65f;
            float half2 = swingDownDuration - half1;
            float t = 0f;
            while (t < half1)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                float a = Mathf.Lerp(swingStartDeg, 0f, EaseInQuad(Mathf.Clamp01(t / half1)));
                if (!fallStarted && a >= -contactLeadDeg)
                {
                    fallStarted = true;
                    StartCoroutine(FallRoutine());
                }
                ApplySwing(a);
                yield return null;
            }
            if (!fallStarted)
            {
                fallStarted = true;
                StartCoroutine(FallRoutine());
            }

            // Phase A2 : dépassement arrière (la pancarte traverse l'emplacement du titre).
            t = 0f;
            while (t < half2)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                ApplySwing(Mathf.Lerp(0f, swingOvershootDeg, EaseOutQuad(Mathf.Clamp01(t / half2))));
                yield return null;
            }

            // Phase B : oscillation amortie jusqu'à la verticale.
            t = 0f;
            float dur = Mathf.Max(0.01f, swingSettleDuration);
            float omega = 3f * Mathf.PI / dur;   // 1,5 cycle
            float decay = 4f / dur;              // amplitude résiduelle ~2 %
            while (t < swingSettleDuration)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                ApplySwing(swingOvershootDeg * Mathf.Exp(-decay * t) * Mathf.Cos(omega * t));
                yield return null;
            }

            // Pose finale exacte, puis le titre couché sort de scène.
            SnapHomeOptions();
            while (!_fallDone) yield return null;
            _signpost.gameObject.SetActive(false);

            IsBusy = false;
            if (onDone != null) onDone();
        }

        IEnumerator SwingOutRoutine(System.Action onDone)
        {
            IsBusy = true;

            // Le titre est censé être couché (fin de SwingIn) : on le réactive tel quel.
            _signpost.gameObject.SetActive(true);

            // La pancarte est remportée vers la caméra (accélérée) ; le titre se relève juste après.
            bool riseStarted = false;
            float t = 0f;
            while (t < swingOutDuration)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                ApplySwing(Mathf.Lerp(0f, swingStartDeg, EaseInQuad(Mathf.Clamp01(t / swingOutDuration))));
                if (!riseStarted && t >= riseDelay)
                {
                    riseStarted = true;
                    StartCoroutine(RiseRoutine());
                }
                yield return null;
            }
            if (!riseStarted) StartCoroutine(RiseRoutine());
            while (!_riseDone) yield return null;

            // États finaux exacts.
            SnapHomeOptions();
            _options.gameObject.SetActive(false);
            SnapHomeSignpost();

            IsBusy = false;
            if (onDone != null) onDone();
        }

        // Chute du titre en arrière : ease-in (gravité) puis rebond léger au sol.
        IEnumerator FallRoutine()
        {
            _fallDone = false;
            float t = 0f;
            while (t < fallDuration)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                ApplyFall(Mathf.Lerp(0f, -fallAngleDeg, EaseInCubic(Mathf.Clamp01(t / fallDuration))));
                yield return null;
            }
            t = 0f;
            while (t < fallBounceDuration)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                float lift = fallBounceDeg * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / fallBounceDuration));
                ApplyFall(-fallAngleDeg + lift);
                yield return null;
            }
            ApplyFall(-fallAngleDeg);
            _fallDone = true;
        }

        // Relèvement du titre avec effet ressort (léger dépassement vers la caméra).
        IEnumerator RiseRoutine()
        {
            _riseDone = false;
            float start = _angleSignpost; // normalement −fallAngleDeg
            float t = 0f;
            while (t < riseDuration)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                t += dt;
                ApplyFall(Mathf.Lerp(start, 0f, EaseOutBack(Mathf.Clamp01(t / riseDuration))));
                yield return null;
            }
            ApplyFall(0f);
            _riseDone = true;
        }

        void ApplySwing(float angle)
        {
            if (_options == null) return;
            _options.RotateAround(_swingPivot, Vector3.right, angle - _angleOptions);
            _angleOptions = angle;
        }

        void ApplyFall(float angle)
        {
            if (_signpost == null) return;
            _signpost.RotateAround(_fallPivot, Vector3.right, angle - _angleSignpost);
            _angleSignpost = angle;
        }

        // États finaux appliqués d'office si une navigation arrive pendant la chorégraphie.
        void ApplySwingInEndState()
        {
            StopAllCoroutines();
            SnapHomeOptions();
            if (_options != null) _options.gameObject.SetActive(true);
            if (_signpost != null)
            {
                SnapHomeSignpost();
                ApplyFall(-fallAngleDeg);
                _signpost.gameObject.SetActive(false);
            }
            _fallDone = true;
            _riseDone = true;
            IsBusy = false;
        }

        void ApplySwingOutEndState()
        {
            StopAllCoroutines();
            if (_signpost != null)
            {
                _signpost.gameObject.SetActive(true);
                SnapHomeSignpost();
            }
            if (_options != null)
            {
                SnapHomeOptions();
                _options.gameObject.SetActive(false);
            }
            _fallDone = true;
            _riseDone = true;
            IsBusy = false;
        }

        static float EaseInQuad(float t) { return t * t; }
        static float EaseOutQuad(float t) { return 1f - (1f - t) * (1f - t); }
        static float EaseInCubic(float t) { return t * t * t; }
        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
