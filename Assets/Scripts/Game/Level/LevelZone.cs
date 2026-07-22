using System;
using Unity.AI.Navigation;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Zone jouable d'un niveau : la grille de construction + tout ce qui doit se
    /// deplacer avec elle (sol NavMesh, spawn ennemi, objectif, village).
    /// Tous ces objets doivent etre ENFANTS du GameObject qui porte ce composant :
    /// deplacer la zone deplace le niveau entier.
    ///
    /// Le joueur la pose ou il veut sur la map via <see cref="LevelZonePlacer"/>,
    /// puis la partie (placement de defenses -> vagues -> boss) se deroule dedans.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelZone : MonoBehaviour
    {
        public static LevelZone Instance { get; private set; }

        [Header("References")]
        [Tooltip("Le composant Grid (cellSize = taille d'une case en unites monde).")]
        [SerializeField] private Grid grid;

        [Tooltip("Le quad/plane qui affiche la grille (mesh Plane Unity = 10x10 unites).")]
        [SerializeField] private Transform visual;

        [Tooltip("Le MeshRenderer du visuel, pour la teinte valide/invalide.")]
        [SerializeField] private MeshRenderer visualRenderer;

        [Tooltip("Le collider sur lequel InputManager raycast pour placer les batiments (layer Placement).")]
        [SerializeField] private Collider placementSurface;

        [Tooltip("Le NavMeshSurface qui bake le sol de la zone. Optionnel.")]
        [SerializeField] private NavMeshSurface navSurface;

        [Header("Dimensions")]
        [Tooltip("Taille de la zone en cases. 40x40 cases de 5 unites = 200x200 unites monde.")]
        [SerializeField] private Vector2Int sizeInCells = new Vector2Int(40, 40);

        [Header("NavMesh")]
        [Tooltip("Rebake le NavMesh (limite au volume de la zone) apres chaque deplacement.")]
        [SerializeField] private bool rebakeNavMeshOnPlace = true;

        [Tooltip("Hauteur du volume de bake, centre sur la zone.")]
        [SerializeField] private float navVolumeHeight = 40f;

        [Header("Rendu")]
        [Tooltip("Ecrit le nombre de cases dans le shader de grille (_Size) pour garder des cases carrees.")]
        [SerializeField] private bool driveShaderSize = true;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SizeId = Shader.PropertyToID("_Size");

        private MaterialPropertyBlock _mpb;
        private GridBlinker _blinker;
        private Color _tint = Color.white;

        /// <summary>Leve quand la zone vient d'etre validee par le joueur.</summary>
        public event Action Placed;

        /// <summary>False tant que le joueur n'a pas valide l'emplacement.</summary>
        public bool IsPlaced { get; private set; }

        public Grid Grid => grid;
        public Vector2Int SizeInCells => sizeInCells;

        /// <summary>Taille d'une case en unites monde (X, Z).</summary>
        public Vector2 CellSize => grid != null
            ? new Vector2(grid.cellSize.x, grid.cellSize.z)
            : Vector2.one;

        /// <summary>Emprise totale de la zone en unites monde (X, Z).</summary>
        public Vector2 WorldSize => new Vector2(sizeInCells.x * CellSize.x, sizeInCells.y * CellSize.y);

        public Vector3 Center => transform.position;
        public float Yaw => transform.eulerAngles.y;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[LevelZone] Plusieurs LevelZone dans la scene, '{name}' est ignoree.", this);
                return;
            }

            Instance = this;
            _mpb = new MaterialPropertyBlock();
            ApplyVisualSize();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnValidate()
        {
            sizeInCells.x = Mathf.Max(1, sizeInCells.x);
            sizeInCells.y = Mathf.Max(1, sizeInCells.y);
            ApplyVisualSize();
        }

        // ---------------------------------------------------------------- placement

        /// <summary>Deplace la zone (centre + rotation autour de Y) sans la valider.</summary>
        public void MoveTo(Vector3 center, float yawDegrees)
        {
            transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yawDegrees, 0f));
        }

        /// <summary>Valide l'emplacement : rebake le NavMesh et previent les systemes.</summary>
        public void Confirm()
        {
            IsPlaced = true;
            SetTint(Color.white);

            if (rebakeNavMeshOnPlace) RebakeNavMesh();

            Placed?.Invoke();
        }

        /// <summary>Repasse la zone en mode "non posee" (relancer un niveau, replacer la zone).</summary>
        public void ResetPlacement()
        {
            IsPlaced = false;
        }

        /// <summary>
        /// Rebake le NavMesh limite au volume de la zone. Indispensable apres
        /// deplacement : sinon les ennemis pathfindent sur l'ancien emplacement.
        /// </summary>
        public void RebakeNavMesh()
        {
            if (navSurface == null) return;

            // Volume centre sur la zone : evite de baker les 7 km de terrain de la map.
            navSurface.collectObjects = CollectObjects.Volume;
            navSurface.center = new Vector3(0f, 0f, 0f);
            navSurface.size = new Vector3(WorldSize.x, navVolumeHeight, WorldSize.y);
            navSurface.BuildNavMesh();
        }

        // ---------------------------------------------------------------- geometrie

        /// <summary>
        /// Vrai si un batiment de <paramref name="footprint"/> cases pose en
        /// <paramref name="cell"/> tient entierement dans la zone.
        /// Convention existante : les cases vont de -size/2 (inclus) a +size/2 (exclu).
        /// </summary>
        public bool ContainsFootprint(Vector3Int cell, Vector2Int footprint)
        {
            int halfX = sizeInCells.x / 2;
            int halfZ = sizeInCells.y / 2;

            return cell.x >= -halfX &&
                   cell.x + footprint.x <= halfX &&
                   cell.z >= -halfZ &&
                   cell.z + footprint.y <= halfZ;
        }

        public bool ContainsCell(Vector3Int cell) => ContainsFootprint(cell, Vector2Int.one);

        /// <summary>Point monde d'une case exprimee en coordonnees locales normalisees [-0.5, 0.5].</summary>
        public Vector3 LocalToWorld(Vector2 normalized)
        {
            Vector3 local = new Vector3(normalized.x * WorldSize.x, 0f, normalized.y * WorldSize.y);
            return transform.TransformPoint(local);
        }

        /// <summary>Les 4 coins de la zone, dans le sens horaire.</summary>
        public void GetCorners(Vector3[] into)
        {
            if (into == null || into.Length < 4) return;
            into[0] = LocalToWorld(new Vector2(-0.5f, -0.5f));
            into[1] = LocalToWorld(new Vector2(-0.5f, 0.5f));
            into[2] = LocalToWorld(new Vector2(0.5f, 0.5f));
            into[3] = LocalToWorld(new Vector2(0.5f, -0.5f));
        }

        // ---------------------------------------------------------------- visuel

        public void SetVisible(bool visible)
        {
            if (visual != null) visual.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Teinte du visuel de grille (vert = emplacement valide, rouge = invalide).
        /// Passe par un MaterialPropertyBlock pour ne pas instancier le materiau ;
        /// <see cref="GridBlinker"/> module l'alpha par-dessus.
        /// </summary>
        public void SetTint(Color color)
        {
            _tint = color;
            if (visualRenderer == null) return;

            // Si un GridBlinker pilote deja _Color (pulse d'alpha), on lui passe la
            // teinte : sinon les deux s'ecraseraient mutuellement chaque frame.
            if (_blinker == null) _blinker = visualRenderer.GetComponent<GridBlinker>();
            if (_blinker != null)
            {
                _blinker.BaseColor = color;
                return;
            }

            _mpb ??= new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, color);
            visualRenderer.SetPropertyBlock(_mpb);
        }

        public Color Tint => _tint;

        /// <summary>
        /// Met l'echelle du plane et le nombre de subdivisions du shader en accord
        /// avec <see cref="sizeInCells"/> : les cases restent carrees quelle que
        /// soit la taille de la zone.
        /// </summary>
        public void ApplyVisualSize()
        {
            if (visual == null) return;

            // Le mesh Plane d'Unity fait 10x10 unites.
            visual.localScale = new Vector3(WorldSize.x / 10f, 1f, WorldSize.y / 10f);

            if (!driveShaderSize || visualRenderer == null) return;
            if (visualRenderer.sharedMaterial == null || !visualRenderer.sharedMaterial.HasProperty(SizeId)) return;

            var block = new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(block);
            block.SetVector(SizeId, new Vector4(sizeInCells.x, sizeInCells.y, 0f, 0f));
            visualRenderer.SetPropertyBlock(block);
        }

        /// <summary>Adapte le collider de placement a l'emprise de la zone.</summary>
        public void ApplyPlacementSurfaceSize()
        {
            if (placementSurface is BoxCollider box)
            {
                box.center = Vector3.zero;
                box.size = new Vector3(WorldSize.x, 0.1f, WorldSize.y);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var corners = new Vector3[4];
            GetCorners(corners);
            Gizmos.color = IsPlaced ? Color.cyan : Color.yellow;
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
    }
}
