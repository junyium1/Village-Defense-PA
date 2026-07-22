using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    /// <summary>
    /// Phase 0 du niveau : le joueur promene la <see cref="LevelZone"/> sur la map
    /// et choisit ou la partie va se derouler.
    ///
    /// Controles :
    ///   souris        -> deplace la zone sur le terrain
    ///   Q / E         -> rotation
    ///   clic gauche   -> valide (uniquement si l'emplacement est valide)
    ///   Echap         -> annule (revient au dernier emplacement valide)
    ///
    /// Validite : on echantillonne une grille de points sur l'emprise de la zone et
    /// on tire un rayon vers le bas. Il faut que TOUS les points touchent le terrain
    /// et que le denivele reste sous <see cref="maxHeightDelta"/> (une zone a cheval
    /// sur une colline donnerait des batiments dans le vide).
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelZonePlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelZone zone;
        [SerializeField] private Camera cam;

        [Tooltip("Le clamping de la camera est suspendu pendant le choix d'emplacement, " +
                 "pour pouvoir survoler toute la map. Optionnel.")]
        [SerializeField] private CameraSystem cameraSystem;

        [Header("Terrain")]
        [Tooltip("Layers consideres comme sol posable (ex: Ground). Le terrain de la map doit avoir un collider sur ce layer.")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("Layers qui bloquent la pose (rochers, batiments existants...). Laisser vide pour desactiver.")]
        [SerializeField] private LayerMask obstacleMask = 0;

        [Header("Validite")]
        [Tooltip("Nombre de points de test par cote (5 = grille de 5x5 = 25 rayons).")]
        [SerializeField, Range(2, 12)] private int samplesPerSide = 5;

        [Tooltip("Denivele maximum tolere sur l'emprise, en unites monde. Plus la zone est " +
                 "grande, plus il faut etre tolerant : utilise 'Village Defense/Zone jouable' " +
                 "> Analyser le terrain pour voir ce que la map permet reellement.")]
        [SerializeField] private float maxHeightDelta = 15f;

        [Tooltip("Hauteur de depart des rayons au-dessus du point vise.")]
        [SerializeField] private float rayStartHeight = 400f;

        [Tooltip("Decalage vertical de la zone au-dessus du sol (evite le z-fighting avec le terrain).")]
        [SerializeField] private float groundOffset = 0.05f;

        [Header("Controles")]
        [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

        [Tooltip("Pas de rotation en degres. 0 = rotation libre et continue.")]
        [SerializeField] private float rotationStep = 15f;

        [Tooltip("Vitesse de rotation en degres/seconde quand rotationStep = 0.")]
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Lissage")]
        [Tooltip("0 = la zone colle instantanement au curseur. Sinon, temps d'amortissement.")]
        [SerializeField, Range(0f, 0.5f)] private float followSmoothing = 0.05f;

        [Header("Couleurs")]
        [SerializeField] private Color validColor = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.25f, 1f);

        /// <summary>Leve quand le joueur valide l'emplacement.</summary>
        public event Action<LevelZone> ZoneConfirmed;

        /// <summary>Leve a chaque changement de validite (utile pour un texte d'aide UI).</summary>
        public event Action<bool> ValidityChanged;

        public bool IsActive { get; private set; }
        public bool IsValid { get; private set; }

        private float _yaw;
        private Vector3 _targetCenter;
        private Vector3 _velocity;
        private bool _hasTarget;
        private Vector3 _lastValidCenter;
        private float _lastValidYaw;
        private bool _hasLastValid;

        private void Awake()
        {
            if (zone == null) zone = LevelZone.Instance;
            if (cam == null) cam = Camera.main;
            if (cameraSystem == null) cameraSystem = FindFirstObjectByType<CameraSystem>();
            enabled = false;
        }

        // ---------------------------------------------------------------- cycle

        /// <summary>Demarre la phase de choix d'emplacement.</summary>
        public void Begin()
        {
            if (zone == null)
            {
                Debug.LogError("[LevelZonePlacer] Aucune LevelZone assignee.", this);
                return;
            }

            if (cam == null) cam = Camera.main;

            zone.ResetPlacement();
            zone.SetVisible(true);

            // La camera doit pouvoir survoler toute la map pour choisir l'endroit.
            if (cameraSystem != null) cameraSystem.SetClampEnabled(false);

            _yaw = zone.Yaw;
            _targetCenter = zone.Center;
            _hasTarget = false;
            _hasLastValid = false;
            IsActive = true;
            enabled = true;
        }

        /// <summary>Sort de la phase sans valider (garde la zone la ou elle est).</summary>
        public void Cancel()
        {
            IsActive = false;
            enabled = false;
            if (zone != null) zone.SetTint(Color.white);
        }

        private void Confirm()
        {
            IsActive = false;
            enabled = false;

            zone.MoveTo(_targetCenter, _yaw);
            zone.Confirm();

            // La camera se recadre sur la zone une fois l'emplacement fige.
            if (cameraSystem != null)
            {
                cameraSystem.SetClampEnabled(true);
                CenterCameraOnZone();
            }

            ZoneConfirmed?.Invoke(zone);
        }

        private void CenterCameraOnZone()
        {
            Transform camRig = cameraSystem.transform;
            Vector3 p = camRig.position;
            camRig.position = new Vector3(zone.Center.x, p.y, zone.Center.z);
        }

        // ---------------------------------------------------------------- boucle

        private void Update()
        {
            if (!IsActive || zone == null || cam == null) return;

            HandleRotation();
            HandleFollow();

            bool wasValid = IsValid;
            IsValid = Evaluate(_targetCenter, _yaw, out float groundY);

            if (IsValid)
            {
                _targetCenter.y = groundY + groundOffset;
                _lastValidCenter = _targetCenter;
                _lastValidYaw = _yaw;
                _hasLastValid = true;
            }

            zone.MoveTo(_targetCenter, _yaw);
            zone.SetTint(IsValid ? validColor : invalidColor);

            if (wasValid != IsValid) ValidityChanged?.Invoke(IsValid);

            HandleConfirmCancel();
        }

        private void HandleRotation()
        {
            bool left = Input.GetKey(rotateLeftKey);
            bool right = Input.GetKey(rotateRightKey);

            if (rotationStep > 0f)
            {
                // Rotation par crans : on ne reagit qu'a l'appui.
                if (Input.GetKeyDown(rotateLeftKey)) _yaw -= rotationStep;
                if (Input.GetKeyDown(rotateRightKey)) _yaw += rotationStep;
            }
            else
            {
                if (left) _yaw -= rotationSpeed * Time.unscaledDeltaTime;
                if (right) _yaw += rotationSpeed * Time.unscaledDeltaTime;
            }

            _yaw = Mathf.Repeat(_yaw, 360f);
        }

        private void HandleFollow()
        {
            if (IsPointerOverUI()) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 10000f, groundMask, QueryTriggerInteraction.Ignore))
                return;

            Vector3 wanted = hit.point;

            if (!_hasTarget || followSmoothing <= 0f)
            {
                _targetCenter = wanted;
                _hasTarget = true;
                _velocity = Vector3.zero;
            }
            else
            {
                _targetCenter = Vector3.SmoothDamp(_targetCenter, wanted, ref _velocity, followSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);
            }
        }

        private void HandleConfirmCancel()
        {
            if (Input.GetKeyDown(cancelKey))
            {
                if (_hasLastValid)
                {
                    _targetCenter = _lastValidCenter;
                    _yaw = _lastValidYaw;
                    zone.MoveTo(_targetCenter, _yaw);
                }
                Cancel();
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;
            if (IsPointerOverUI()) return;
            if (!IsValid) return;

            Confirm();
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // ---------------------------------------------------------------- validite

        /// <summary>
        /// Teste l'emprise de la zone centree en <paramref name="center"/> avec la
        /// rotation <paramref name="yaw"/>. Renvoie la hauteur de sol moyenne dans
        /// <paramref name="groundY"/>.
        /// </summary>
        public bool Evaluate(Vector3 center, float yaw, out float groundY)
        {
            groundY = center.y;

            Vector2 size = zone.WorldSize;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            int n = Mathf.Max(2, samplesPerSide);
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            float sumY = 0f;
            int hits = 0;

            for (int ix = 0; ix < n; ix++)
            {
                for (int iz = 0; iz < n; iz++)
                {
                    float u = (float)ix / (n - 1) - 0.5f;
                    float v = (float)iz / (n - 1) - 0.5f;

                    Vector3 local = new Vector3(u * size.x, 0f, v * size.y);
                    Vector3 world = center + rot * local;
                    Vector3 origin = new Vector3(world.x, center.y + rayStartHeight, world.z);

                    if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                        return false; // un coin dans le vide -> emplacement refuse

                    float y = hit.point.y;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    sumY += y;
                    hits++;
                }
            }

            if (hits == 0) return false;

            groundY = sumY / hits;

            if (maxY - minY > maxHeightDelta) return false;

            if (obstacleMask.value != 0)
            {
                Vector3 halfExtents = new Vector3(size.x * 0.5f, maxHeightDelta * 0.5f + 0.5f, size.y * 0.5f);
                Vector3 boxCenter = new Vector3(center.x, groundY + halfExtents.y, center.z);
                if (Physics.CheckBox(boxCenter, halfExtents, rot, obstacleMask, QueryTriggerInteraction.Ignore))
                    return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (zone == null) return;
            var corners = new Vector3[4];
            zone.GetCorners(corners);
            Gizmos.color = IsValid ? Color.green : Color.red;
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
    }
}
