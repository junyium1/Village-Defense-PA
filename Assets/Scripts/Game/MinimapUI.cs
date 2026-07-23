using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Defenses;
using Game.Units;

namespace Game
{
    /// <summary>
    /// Minimap schematique temps reel de la LevelZone.
    /// Rond rouge = ennemi, rond vert = allie, carre vert = batiment (tourelle ou piege).
    /// S'auto-instancie dans toute scene contenant une LevelZone (aucun cablage de scene).
    /// Touche M = afficher/masquer.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        // --------------------------------------------------------------- champs serialises

        [Tooltip("Taille du panneau de la minimap en pixels (reference 1920x1080).")]
        [SerializeField] float panelSize = 208f;

        [Tooltip("Marge bas-gauche du panneau par rapport au coin de l'ecran.")]
        [SerializeField] Vector2 margin = new Vector2(24f, 24f);

        [Tooltip("Intervalle de rafraichissement des icones (secondes).")]
        [SerializeField] float refreshInterval = 0.15f;

        [Tooltip("Taille d'une icone (pixels).")]
        [SerializeField] float iconSize = 7f;

        [Tooltip("Couleur des icones ennemies.")]
        [SerializeField] Color enemyColor = new Color(0.95f, 0.25f, 0.22f);

        [Tooltip("Couleur des icones alliees.")]
        [SerializeField] Color allyColor = new Color(0.35f, 0.9f, 0.4f);

        [Tooltip("Couleur des icones de batiments (tourelles/pieges).")]
        [SerializeField] Color buildingColor = new Color(0.35f, 0.9f, 0.4f);

        [Tooltip("Couleur de fond du panneau.")]
        [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

        [Tooltip("Couleur des lignes de la grille.")]
        [SerializeField] Color gridLineColor = new Color(1f, 1f, 1f, 0.08f);

        [Tooltip("Couleur de la bordure du panneau.")]
        [SerializeField] Color borderColor = new Color(1f, 1f, 1f, 0.3f);

        // --------------------------------------------------------------- constantes

        const int IconPoolSize = 160;

        // --------------------------------------------------------------- prives

        LevelZone _zone;
        GameObject _root;
        RectTransform _mapArea;
        readonly List<Image> _pool = new List<Image>();
        int _used;
        float _nextRefresh;
        bool _visible = true;
        bool _built;
        static Sprite _circleSprite;
        static Sprite _gridSprite;
        static Vector2Int _gridCells = new Vector2Int(-1, -1);

        // --------------------------------------------------------------- auto-spawn

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;   // anti double-abonnement (fast enter play)
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LevelZone zone = LevelZone.Instance;
            if (zone == null) zone = FindAnyObjectByType<LevelZone>();
            if (zone == null) return;                    // pas de niveau (menu) : pas de minimap
            if (FindAnyObjectByType<MinimapUI>() != null) return;
            MinimapUI ui = new GameObject("MinimapUI").AddComponent<MinimapUI>();
            ui._zone = zone;
        }

        // --------------------------------------------------------------- Update

        void Update()
        {
            // Toggle visibilite via touche M.
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame)
            {
                _visible = !_visible;
                if (_root != null) _root.SetActive(_visible);
            }

            // Construction differee si la zone n'etait pas prete au spawn.
            if (!_built)
            {
                if (_zone == null)
                {
                    _zone = LevelZone.Instance;
                    if (_zone == null) _zone = FindAnyObjectByType<LevelZone>();
                }
                if (_zone == null) return;
                Build();
                _built = true;
            }

            if (!_visible) return;

            // Timer de rafraichissement (unscaled pour rester stable en pause).
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + refreshInterval;
            RefreshIcons();
        }

        // --------------------------------------------------------------- Build

        void Build()
        {
            // 1. Canvas overlay.
            _root = new GameObject("MinimapCanvas");
            _root.transform.SetParent(transform);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Pas de GraphicRaycaster : aucune interaction souris/clavier sur la minimap.

            // 2. Panneau de fond.
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_root.transform, false);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.zero;
            panelRt.pivot = Vector2.zero;
            panelRt.anchoredPosition = margin;
            panelRt.sizeDelta = new Vector2(panelSize, panelSize);
            Image panelImg = panel.GetComponent<Image>();
            panelImg.sprite = null;
            panelImg.color = backgroundColor;
            panelImg.raycastTarget = false;

            // 3. Grille (sprite genere proceduralement).
            GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(Image));
            grid.transform.SetParent(panel.transform, false);
            RectTransform gridRt = grid.GetComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = Vector2.one;
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;
            Image gridImg = grid.GetComponent<Image>();
            gridImg.sprite = GenerateGridSprite(_zone.SizeInCells);
            gridImg.color = Color.white;
            gridImg.raycastTarget = false;

            // 4. Conteneur d'icones (stretche sur le panneau).
            GameObject icons = new GameObject("Icons", typeof(RectTransform));
            icons.transform.SetParent(panel.transform, false);
            _mapArea = icons.GetComponent<RectTransform>();
            _mapArea.anchorMin = Vector2.zero;
            _mapArea.anchorMax = Vector2.one;
            _mapArea.offsetMin = Vector2.zero;
            _mapArea.offsetMax = Vector2.zero;

            // 5. Visibilite initiale.
            _root.SetActive(_visible);
        }

        // --------------------------------------------------------------- sprites proceduraux

        /// <summary>
        /// Genere un sprite de grille 256x256 avec les lignes de cellules
        /// et une bordure exterieure.
        /// </summary>
        Sprite GenerateGridSprite(Vector2Int cells)
        {
            // Cache statique : evite de fuiter 256 Ko de texture a chaque chargement
            // de la scene de jeu (le sprite survit au changement de scene).
            if (_gridSprite != null && _gridCells == cells) return _gridSprite;

            const int texSize = 256;
            Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;

            // Fond transparent.
            Color[] pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            // Lignes verticales internes (pas de trait aux bords, la bordure s'en charge).
            for (int i = 1; i < cells.x; i++)
            {
                int x = Mathf.RoundToInt((float)i / cells.x * texSize);
                if (x < 0 || x >= texSize) continue;
                for (int y = 0; y < texSize; y++)
                    pixels[y * texSize + x] = gridLineColor;
            }

            // Lignes horizontales internes.
            for (int j = 1; j < cells.y; j++)
            {
                int y = Mathf.RoundToInt((float)j / cells.y * texSize);
                if (y < 0 || y >= texSize) continue;
                for (int x = 0; x < texSize; x++)
                    pixels[y * texSize + x] = gridLineColor;
            }

            // Bordure exterieure de 2 px.
            for (int b = 0; b < 2; b++)
            {
                int lo = b;
                int hi = texSize - 1 - b;
                for (int x = 0; x < texSize; x++)
                {
                    pixels[lo * texSize + x] = borderColor;
                    pixels[hi * texSize + x] = borderColor;
                }
                for (int y = 0; y < texSize; y++)
                {
                    pixels[y * texSize + lo] = borderColor;
                    pixels[y * texSize + hi] = borderColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite spr = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 100f);
            spr.hideFlags = HideFlags.HideAndDontSave;
            _gridSprite = spr;
            _gridCells = cells;
            return spr;
        }

        /// <summary>
        /// Genere (lazy) un sprite de cercle blanc 32x32 avec fondu de bord.
        /// </summary>
        Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            const float center = 15.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(15f - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _circleSprite.hideFlags = HideFlags.HideAndDontSave;
            return _circleSprite;
        }

        // --------------------------------------------------------------- rafraichissement

        void RefreshIcons()
        {
            if (_zone == null) return;
            _used = 0;

            // Batiments d'abord (dessous) : carres verts.
            // Une Image sans sprite rend un rectangle plein de la couleur demandee.
            foreach (TurretManager t in TurretManager.All)
                if (t != null) PlaceIcon(t.transform.position, null, buildingColor);
            foreach (TrapManager t in TrapManager.All)
                if (t != null) PlaceIcon(t.transform.position, null, buildingColor);

            // Allies ensuite.
            foreach (Unit u in Unit.All)
            {
                if (u == null || u.data == null) continue;
                if (u.GetFaction() == Faction.Ally) PlaceIcon(u.transform.position, GetCircleSprite(), allyColor);
            }

            // Ennemis en dernier (au-dessus en cas de superposition).
            foreach (Unit u in Unit.All)
            {
                if (u == null || u.data == null) continue;
                if (u.GetFaction() == Faction.Enemy) PlaceIcon(u.transform.position, GetCircleSprite(), enemyColor);
            }

            // Les icones non utilisees ce tick repassent en sommeil.
            for (int i = _used; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Place (ou reutilise) une icone a la position monde donnee.
        /// </summary>
        void PlaceIcon(Vector3 worldPos, Sprite sprite, Color color)
        {
            Vector3 local = _zone.transform.InverseTransformPoint(worldPos);
            Vector2 size = _zone.WorldSize;
            if (size.x <= 0f || size.y <= 0f) return;

            float nx = local.x / size.x + 0.5f;
            float ny = local.z / size.y + 0.5f;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f) return;   // hors zone : pas d'icone

            Image icon = Acquire();
            if (icon == null) return;                                // pool saturee : on saute

            icon.sprite = sprite;
            icon.color = color;
            icon.rectTransform.anchoredPosition = new Vector2(nx * panelSize, ny * panelSize);
        }

        /// <summary>
        /// Recupere une icone du pool (reuse si disponible, sinon cree jusqu'a la limite).
        /// </summary>
        Image Acquire()
        {
            if (_used < _pool.Count)
            {
                Image reused = _pool[_used++];
                reused.gameObject.SetActive(true);
                reused.transform.SetAsLastSibling();   // l'ordre d'appel = l'ordre d'empilage
                return reused;
            }
            if (_pool.Count >= IconPoolSize) return null;

            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapArea, false);
            Image icon = go.GetComponent<Image>();
            icon.raycastTarget = false;
            RectTransform rt = icon.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            _pool.Add(icon);
            _used++;
            return icon;
        }
    }
}
