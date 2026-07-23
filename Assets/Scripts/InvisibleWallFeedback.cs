using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Retour visuel de proximite sur les murs invisibles.
///
/// A poser sur le parent des murs (InvisibleBorders). Le composant deduit la
/// boite de jeu des BoxCollider enfants, puis genere un quad transparent par
/// face. Plus la camera s'approche d'une face, plus un halo blanc s'y allume,
/// centre sur le point de la face le plus proche de la camera.
///
/// La boite est aussi exposee via <see cref="WorldBounds"/> : <see cref="CameraSystem"/>
/// s'en sert comme limite dure, ce qui empeche la camera de traverser les murs.
/// </summary>
[DisallowMultipleComponent]
public class InvisibleWallFeedback : MonoBehaviour
{
    [Header("Cible suivie")]
    [Tooltip("Transform dont on mesure la distance aux murs. Vide = Camera.main.")]
    [SerializeField] private Transform tracked;

    [Header("Halo")]
    [SerializeField] private Color glowColor = Color.white;

    [Tooltip("Distance a laquelle le halo commence a apparaitre, en unites monde.")]
    [SerializeField] private float fadeDistance = 40f;

    [Tooltip("Intensite du halo quand on colle au mur. Le rendu etant additif, " +
             "au-dela de 1 le blanc sature de plus en plus vite.")]
    [SerializeField, Range(0f, 3f)] private float maxAlpha = 1.8f;

    [Tooltip("Rayon du halo sur le mur, en unites monde.")]
    [SerializeField] private float glowRadius = 55f;

    [Tooltip("Adoucissement des bords du quad, en fraction de la face.")]
    [SerializeField, Range(0.001f, 0.5f)] private float edgeFade = 0.12f;

    [Tooltip("Intensite en fonction de la distance. x = 0 (colle au mur) -> 1 (a fadeDistance).")]
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Faces")]
    [SerializeField] private bool showWalls = true;
    [SerializeField] private bool showCeiling = true;
    [SerializeField] private bool showFloor = false;

    [Header("Materiau")]
    [Tooltip("Laisser vide pour instancier automatiquement VillageDefense/InvisibleWallGlow.")]
    [SerializeField] private Material glowMaterial;

    /// <summary>Boite monde englobant tous les murs. Valide des Awake.</summary>
    public Bounds WorldBounds => _bounds;

    /// <summary>False si aucun BoxCollider enfant n'a ete trouve.</summary>
    public bool HasBounds => _hasBounds;

    private Bounds _bounds;
    private bool _hasBounds;

    private readonly List<Face> _faces = new List<Face>();
    private Material _runtimeMaterial;
    private Mesh _quadMesh;

    /// <summary>Une face de la boite : son quad, sa normale sortante et sa taille.</summary>
    private struct Face
    {
        public Vector3 outward;   // normale sortante, en monde
        public Vector3 center;    // centre de la face, en monde
        public Transform quad;
        public MeshRenderer renderer;

        // Instance de materiau dediee. On n'utilise PAS de MaterialPropertyBlock :
        // le SRP Batcher de URP les ignore des que les proprietes sont dans le
        // CBUFFER UnityPerMaterial, et le halo resterait invisible.
        public Material material;

        public float width;       // taille le long de l'axe X local du quad
        public float height;      // taille le long de l'axe Y local du quad
    }

    private void Awake()
    {
        RecomputeBounds();
    }

    private void Start()
    {
        if (tracked == null && Camera.main != null) tracked = Camera.main.transform;
        BuildFaces();
    }

    private void OnDestroy()
    {
        foreach (Face f in _faces)
            if (f.material != null) Destroy(f.material);

        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        if (_quadMesh != null) Destroy(_quadMesh);
    }

    /// <summary>
    /// Recalcule la boite a partir des BoxCollider enfants. A rappeler si les
    /// murs sont redimensionnes a chaud.
    /// </summary>
    public void RecomputeBounds()
    {
        var colliders = GetComponentsInChildren<BoxCollider>(includeInactive: true);
        _hasBounds = false;

        foreach (var c in colliders)
        {
            if (!_hasBounds)
            {
                _bounds = c.bounds;
                _hasBounds = true;
            }
            else
            {
                _bounds.Encapsulate(c.bounds);
            }
        }

        if (!_hasBounds)
            Debug.LogWarning($"[{nameof(InvisibleWallFeedback)}] Aucun BoxCollider enfant : pas de boite.", this);
    }

    // ---------------------------------------------------------------- montage

    private void BuildFaces()
    {
        if (!_hasBounds) return;

        _quadMesh = CreateUnitQuad();

        Material mat = glowMaterial;
        if (mat == null)
        {
            Shader shader = Shader.Find("VillageDefense/InvisibleWallGlow");
            if (shader == null)
            {
                Debug.LogError($"[{nameof(InvisibleWallFeedback)}] Shader 'VillageDefense/InvisibleWallGlow' introuvable.", this);
                return;
            }
            _runtimeMaterial = new Material(shader) { name = "InvisibleWallGlow (runtime)" };
            mat = _runtimeMaterial;
        }

        Vector3 size = _bounds.size;
        Vector3 c = _bounds.center;

        if (showWalls)
        {
            AddFace(mat, "Glow_East",  Vector3.right,   new Vector3(_bounds.max.x, c.y, c.z), Vector3.up,      size.z, size.y);
            AddFace(mat, "Glow_West",  Vector3.left,    new Vector3(_bounds.min.x, c.y, c.z), Vector3.up,      size.z, size.y);
            AddFace(mat, "Glow_North", Vector3.forward, new Vector3(c.x, c.y, _bounds.max.z), Vector3.up,      size.x, size.y);
            AddFace(mat, "Glow_South", Vector3.back,    new Vector3(c.x, c.y, _bounds.min.z), Vector3.up,      size.x, size.y);
        }

        if (showCeiling)
            AddFace(mat, "Glow_Ceiling", Vector3.up,   new Vector3(c.x, _bounds.max.y, c.z), Vector3.forward, size.x, size.z);

        if (showFloor)
            AddFace(mat, "Glow_Floor",   Vector3.down, new Vector3(c.x, _bounds.min.y, c.z), Vector3.forward, size.x, size.z);
    }

    /// <summary>
    /// Cree le quad d'une face. Le quad regarde vers l'interieur de la boite et
    /// est mis a l'echelle pour couvrir exactement la face.
    /// </summary>
    private void AddFace(Material mat, string name, Vector3 outward, Vector3 center, Vector3 up, float width, float height)
    {
        var go = new GameObject(name)
        {
            hideFlags = HideFlags.DontSave // genere au runtime : ne doit pas polluer la scene
        };
        go.layer = gameObject.layer;

        Transform t = go.transform;
        t.SetParent(transform, worldPositionStays: false);
        t.position = center;
        t.rotation = Quaternion.LookRotation(-outward, up);
        t.localScale = new Vector3(width, height, 1f);

        go.AddComponent<MeshFilter>().sharedMesh = _quadMesh;

        var faceMat = new Material(mat) { name = mat.name + " (" + name + ")" };

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = faceMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        mr.enabled = false; // allume seulement quand la camera approche

        _faces.Add(new Face
        {
            outward = outward,
            center = center,
            quad = t,
            renderer = mr,
            material = faceMat,
            width = width,
            height = height,
        });
    }

    private static Mesh CreateUnitQuad()
    {
        var mesh = new Mesh { name = "InvisibleWallQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }

    // ---------------------------------------------------------------- boucle

    private void LateUpdate()
    {
        if (_faces.Count == 0) return;

        if (tracked == null)
        {
            if (Camera.main == null) return;
            tracked = Camera.main.transform;
        }

        Vector3 camPos = tracked.position;

        for (int i = 0; i < _faces.Count; i++)
        {
            Face f = _faces[i];

            // Distance signee a la face : positive quand on est du bon cote (dedans).
            // Si la camera est deja dehors, on sature a 0 -> halo au maximum.
            float dist = Mathf.Max(0f, -Vector3.Dot(camPos - f.center, f.outward));

            float t = fadeDistance <= 0f ? 0f : Mathf.Clamp01(dist / fadeDistance);
            float strength = Mathf.Max(0f, falloff.Evaluate(t)) * maxAlpha;

            if (strength <= 0.001f)
            {
                if (f.renderer.enabled) f.renderer.enabled = false;
                continue;
            }

            if (!f.renderer.enabled) f.renderer.enabled = true;

            // Projection de la camera sur le plan de la face, en UV du quad.
            Vector3 local = f.quad.InverseTransformPoint(camPos);
            Vector2 uv = new Vector2(local.x + 0.5f, local.y + 0.5f);

            f.material.SetColor("_GlowColor", glowColor);
            f.material.SetVector("_GlowCenter", new Vector4(uv.x, uv.y, 0f, 0f));
            f.material.SetFloat("_GlowRadius", glowRadius / Mathf.Max(f.height, 1e-4f));
            f.material.SetFloat("_GlowStrength", strength);
            f.material.SetFloat("_Aspect", f.width / Mathf.Max(f.height, 1e-4f));
            f.material.SetFloat("_EdgeFade", edgeFade);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) RecomputeBounds();
        if (!_hasBounds) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        Gizmos.DrawWireCube(_bounds.center, _bounds.size);
    }
}
