using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Menus
{
    /// <summary>
    /// Écran « Touches » (Options → Touches) : pancarte 2D en pixel art
    /// (Art/pixelsarts/pencarte) listant les six déplacements caméra —
    /// Devant / Derrière / Gauche / Droite / Haut / Bas — et permettant de les
    /// rebinder. Lit/écrit <see cref="KeybindStore"/> (persisté PlayerPrefs).
    ///
    /// Toute l'UI est construite par code au premier affichage : le seul câblage
    /// de scène nécessaire est le sprite de la pancarte (repli automatique sur
    /// Resources/UI/pencarte, puis sur un aplat si le sprite manque).
    /// Fonctionne aussi en pause (timeScale = 0) : superposition indépendante des
    /// pancartes 3D, dont l'input est suspendu tant que l'écran est ouvert.
    /// </summary>
    public class KeybindsScreen : MonoBehaviour
    {
        public static KeybindsScreen Instance { get; private set; }

        /// <summary>Vrai tant que l'écran est affiché (Echap lui appartient).</summary>
        public static bool IsOpen { get { return Instance != null && Instance._open; } }

        [Tooltip("Pancarte pixel art servant de fond (Art/pixelsarts/pencarte).\n" +
                 "Vide = chargée depuis Resources/UI/pencarte.")]
        [SerializeField] Sprite panelSprite;

        [Tooltip("Largeur de la pancarte, en pixels de la résolution de référence (1920x1080).")]
        [SerializeField] float panelWidth = 1240f;

        // Zone de bois utile dans le sprite (mesurée sur pencarte.png, marge comprise) :
        // le cadre et les chaînes restent libres de tout contenu.
        static readonly Vector2 BoardAnchorMin = new Vector2(0.243f, 0.168f);
        static readonly Vector2 BoardAnchorMax = new Vector2(0.757f, 0.727f);

        const string SpriteResourcePath = "UI/pencarte";
        const string CanvasRootName = "KeybindsCanvas";

        static readonly Color TextColor = new Color32(0xF5, 0xE6, 0xC8, 0xFF);
        static readonly Color KeyColor = new Color32(0x3A, 0x28, 0x1C, 0xFF);
        static readonly Color KeyHoverColor = new Color32(0x5C, 0x3F, 0x2A, 0xFF);
        static readonly Color WaitingColor = new Color32(0xFF, 0xC2, 0x4D, 0xFF);
        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);

        readonly TextMeshProUGUI[] _keyLabels = new TextMeshProUGUI[KeybindStore.ActionCount];

        GameObject _root;
        bool _open;
        int _awaiting = -1;          // index de l'action en cours de rebind, -1 = aucune
        Menu3DInput _blockedInput;   // input 3D suspendu pendant l'affichage

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            // _root n'est pas sérialisé : même si « KeybindsCanvas » existe déjà comme
            // enfant sauvegardé dans la scène (construit dans l'éditeur pour le
            // redimensionner), la référence est perdue au chargement — on la retrouve ici.
            TryLinkExistingRoot();

            // Sécurité : si cet enfant a été laissé visible dans l'éditeur, on le force
            // fermé au lancement — Show() le rouvrira normalement au clic sur « Touches ».
            if (_root != null && !_open) _root.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                KeybindStore.Changed -= RefreshLabels;
                Instance = null;
            }
        }

        // ----------------------- ouverture / fermeture -----------------------

        /// <summary>Ouvre l'écran Touches (crée l'instance si la scène n'en contient pas).</summary>
        public static void Open()
        {
            KeybindsScreen screen = Instance;
            if (screen == null)
            {
                screen = FindAnyObjectByType<KeybindsScreen>(FindObjectsInactive.Include);
                if (screen == null)
                    screen = new GameObject("KeybindsScreen").AddComponent<KeybindsScreen>();
                Instance = screen;
            }
            screen.Show();
        }

        public void Show()
        {
            if (_open) return;
            _open = true;
            _awaiting = -1;

            EnsureRoot();
            _root.SetActive(true);

            // Les planches 3D sont derrière l'écran : leur raycast reste actif sinon
            // un clic sur un bouton traverserait jusqu'à la pancarte.
            _blockedInput = FindAnyObjectByType<Menu3DInput>();
            if (_blockedInput != null) _blockedInput.enabled = false;

            KeybindStore.Changed += RefreshLabels;
            RefreshLabels();
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _awaiting = -1;

            KeybindStore.Changed -= RefreshLabels;
            if (_root != null) _root.SetActive(false);

            if (_blockedInput != null)
            {
                _blockedInput.enabled = true;
                _blockedInput = null;
            }
        }

        // ----------------------- construction / réutilisation de la racine -----------------------

        /// <summary>Construit l'UI si besoin, ou réutilise « KeybindsCanvas » s'il existe déjà
        /// comme enfant sauvegardé dans la scène (construit dans l'éditeur, cf. <see cref="BuildInEditor"/>).</summary>
        void EnsureRoot()
        {
            if (_root != null) return;
            TryLinkExistingRoot();
            if (_root == null) Build();
        }

        void TryLinkExistingRoot()
        {
            if (_root != null) return;
            Transform existing = transform.Find(CanvasRootName);
            if (existing == null) return;
            _root = existing.gameObject;
            CacheLabelReferences();
            RewireButtons();
        }

        /// <summary>Recâble les onClick d'un « KeybindsCanvas » sauvegardé dans la scène :
        /// les listeners ajoutés par code dans <see cref="Build"/> ne sont PAS sérialisés —
        /// sans ce recâblage, Retour / Par défaut / les touches ne répondent plus au clic.</summary>
        void RewireButtons()
        {
            Transform board = _root.transform.Find("Panel/Board");
            if (board == null) return;

            for (int i = 0; i < KeybindStore.ActionCount; i++)
            {
                Transform row = board.Find("Row_" + (KeybindAction)i);
                if (row == null) continue;
                Transform key = row.Find("Key");
                if (key == null) continue;
                Button button = key.GetComponent<Button>();
                if (button == null) continue;
                int captured = i;   // capture explicite : la lambda survit à la boucle
                button.onClick.AddListener(() => BeginRebind(captured));
            }

            Transform footer = board.Find("Footer");
            if (footer == null) return;

            Transform defaults = footer.Find("Defaults");
            if (defaults != null)
            {
                Button defaultsButton = defaults.GetComponent<Button>();
                if (defaultsButton != null)
                    defaultsButton.onClick.AddListener(() =>
                    {
                        _awaiting = -1;
                        KeybindStore.ResetToDefaults();
                        RefreshLabels();
                    });
            }

            Transform back = footer.Find("Back");
            if (back != null)
            {
                Button backButton = back.GetComponent<Button>();
                if (backButton != null)
                    backButton.onClick.AddListener(Close);
            }
        }

        /// <summary>Retrouve les labels de touche par chemin, après réutilisation d'un
        /// « KeybindsCanvas » existant (les références de <see cref="Build"/> sont perdues
        /// au rechargement : <see cref="_keyLabels"/> n'est pas sérialisé).</summary>
        void CacheLabelReferences()
        {
            Transform board = _root.transform.Find("Panel/Board");
            if (board == null) return;

            for (int i = 0; i < _keyLabels.Length; i++)
            {
                Transform row = board.Find("Row_" + (KeybindAction)i);
                Transform key = row != null ? row.Find("Key") : null;
                _keyLabels[i] = key != null ? key.GetComponentInChildren<TextMeshProUGUI>() : null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Construire dans la scène (pour redimensionner)")]
        void BuildInEditor()
        {
            EnsureRoot();
            _root.SetActive(true);

            Transform panel = _root.transform.Find("Panel");
            UnityEditor.Selection.activeGameObject = panel != null ? panel.gameObject : _root;

            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        // ----------------------- capture de touche -----------------------

        void Update()
        {
            if (!_open) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (_awaiting < 0)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) Close();
                return;
            }

            // Echap annule la capture au lieu d'être assignée : sans ça le joueur
            // pourrait binder Echap et ne plus pouvoir sortir du menu.
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _awaiting = -1;
                RefreshLabels();
                return;
            }

            foreach (KeyControl control in keyboard.allKeys)
            {
                if (!control.wasPressedThisFrame) continue;
                KeybindStore.Set((KeybindAction)_awaiting, control.keyCode);
                _awaiting = -1;
                RefreshLabels();   // KeybindStore.Changed rafraîchit déjà, mais pas l'état « en attente »
                return;
            }
        }

        void BeginRebind(int actionIndex)
        {
            _awaiting = actionIndex;
            RefreshLabels();
        }

        void RefreshLabels()
        {
            for (int i = 0; i < _keyLabels.Length; i++)
            {
                TextMeshProUGUI label = _keyLabels[i];
                if (label == null) continue;

                bool waiting = i == _awaiting;
                label.text = waiting ? "..." : KeybindStore.DisplayName((KeybindAction)i);
                label.color = waiting ? WaitingColor : TextColor;
            }
        }

        // ----------------------- construction de l'UI -----------------------

        void Build()
        {
            EnsureEventSystem();

            _root = new GameObject(CanvasRootName,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;   // au-dessus des HUD existants

            CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Voile plein écran : assombrit et absorbe les clics hors pancarte.
            Image dim = NewImage("Dim", _root.transform, DimColor, null);
            Stretch(dim.rectTransform);

            Sprite sprite = ResolveSprite();
            Image panel = NewImage("Panel", _root.transform,
                sprite != null ? Color.white : new Color32(0x6B, 0x45, 0x2B, 0xFF), sprite);
            panel.preserveAspect = true;
            panel.raycastTarget = false;

            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            float ratio = sprite != null ? sprite.rect.height / sprite.rect.width : 369f / 677f;
            panelRect.sizeDelta = new Vector2(panelWidth, panelWidth * ratio);

            // Contenu cantonné à la planche de bois du sprite.
            RectTransform board = NewRect("Board", panelRect);
            board.anchorMin = BoardAnchorMin;
            board.anchorMax = BoardAnchorMax;
            board.offsetMin = Vector2.zero;
            board.offsetMax = Vector2.zero;

            VerticalLayoutGroup column = board.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            TextMeshProUGUI title = NewText("Title", board, "TOUCHES", 34f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            SetHeight(title.gameObject, 44f);

            for (int i = 0; i < KeybindStore.ActionCount; i++)
                BuildRow(board, i);

            BuildFooter(board);

            SetLayerRecursively(_root, LayerMask.NameToLayer("UI"));
            _root.SetActive(false);
        }

        void BuildRow(RectTransform parent, int actionIndex)
        {
            var action = (KeybindAction)actionIndex;

            RectTransform row = NewRect("Row_" + action, parent);
            SetHeight(row.gameObject, 40f);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            TextMeshProUGUI label = NewText("Label", row, KeybindStore.LabelOf(action), 26f, FontStyles.Normal);
            label.alignment = TextAlignmentOptions.Left;
            SetFlexibleWidth(label.gameObject, 1f);

            int captured = actionIndex;   // capture explicite : la lambda survit à la boucle
            Button button = NewButton("Key", row, "", 24f, () => BeginRebind(captured));
            SetWidth(button.gameObject, 200f);
            _keyLabels[actionIndex] = button.GetComponentInChildren<TextMeshProUGUI>();
        }

        void BuildFooter(RectTransform parent)
        {
            RectTransform footer = NewRect("Footer", parent);
            SetHeight(footer.gameObject, 44f);

            HorizontalLayoutGroup layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            NewButton("Defaults", footer, "Par défaut", 22f, () =>
            {
                _awaiting = -1;
                KeybindStore.ResetToDefaults();
                RefreshLabels();
            });
            NewButton("Back", footer, "Retour", 22f, Close);
        }

        Sprite ResolveSprite()
        {
            if (panelSprite != null) return panelSprite;
            panelSprite = Resources.Load<Sprite>(SpriteResourcePath);
            if (panelSprite == null)
                Debug.LogWarning("[KeybindsScreen] Sprite de pancarte introuvable (champ vide et Resources/" +
                                 SpriteResourcePath + " absent) — repli sur un aplat de bois.");
            return panelSprite;
        }

        static void EnsureEventSystem()
        {
            // EventSystem.current reste null hors Play (OnEnable ne tourne pas en mode
            // édition) : on cherche l'objet lui-même pour ne pas en dupliquer un en
            // construisant l'écran dans l'éditeur (cf. BuildInEditor).
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            go.transform.SetParent(null);
        }

        // ----------------------- petits helpers UI -----------------------

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, string content, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static Button NewButton(string name, Transform parent, string content, float size, UnityEngine.Events.UnityAction onClick)
        {
            // Le teintage du Button multiplie la couleur du graphique : on laisse
            // l'image en blanc pour que les couleurs d'état s'affichent telles quelles.
            Image background = NewImage(name, parent, Color.white, null);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = KeyColor;
            colors.highlightedColor = KeyHoverColor;
            colors.pressedColor = new Color(KeyHoverColor.r * 0.8f, KeyHoverColor.g * 0.8f, KeyHoverColor.b * 0.8f, 1f);
            colors.selectedColor = KeyColor;
            colors.disabledColor = KeyColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            TextMeshProUGUI label = NewText("Label", background.transform, content, size, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            Stretch(label.rectTransform);

            return button;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return;   // couche « UI » absente du projet : on ne touche à rien
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static LayoutElement LayoutOf(GameObject go)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            return element != null ? element : go.AddComponent<LayoutElement>();
        }

        static void SetHeight(GameObject go, float height) { LayoutOf(go).preferredHeight = height; }
        static void SetWidth(GameObject go, float width) { LayoutOf(go).preferredWidth = width; }
        static void SetFlexibleWidth(GameObject go, float weight) { LayoutOf(go).flexibleWidth = weight; }
    }
}
