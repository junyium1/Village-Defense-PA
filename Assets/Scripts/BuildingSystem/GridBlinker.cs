using UnityEngine;

/// <summary>
/// Fait clignoter (pulse) le materiau de la grille de placement.
/// Utilise unscaledTime pour survivre au timeScale=0 (pause).
/// Attacher sur le GameObject qui porte le MeshRenderer de la grille.
///
/// La TEINTE est pilotable de l'exterieur via <see cref="BaseColor"/> :
/// LevelZone s'en sert pour passer la grille en vert/rouge pendant le choix
/// de l'emplacement, sans que les deux systemes se marchent dessus sur _Color.
/// </summary>
public class GridBlinker : MonoBehaviour
{
    [Header("Clignotement")]
    [SerializeField, Range(0.1f, 5f)] private float speed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float alphaMin = 0.08f;
    [SerializeField, Range(0f, 1f)] private float alphaMax = 0.35f;

    [Header("Couleur de la grille")]
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 1f);

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Teinte de base (l'alpha est ecrase par le pulse).</summary>
    public Color BaseColor
    {
        get => gridColor;
        set => gridColor = value;
    }

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (_renderer == null) return;

        // Pulse sinusoïdal : oscille entre alphaMin et alphaMax
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(alphaMin, alphaMax, t);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorId, new Color(gridColor.r, gridColor.g, gridColor.b, alpha));
        _renderer.SetPropertyBlock(_mpb);
    }
}
