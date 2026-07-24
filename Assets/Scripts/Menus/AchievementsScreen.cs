using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game;

namespace Menus
{
    // Ecran « Succes » : pancarte 2D en pixel art listant les 4 succes du jeu.
    // S'auto-instancie dans MainMenuScene (aucun cablage de scene requis).
    // Ouverture : touche Echap (si aucun autre ecran overlay n'est deja ouvert).
    // Fermeture : Echap ou bouton FERMER.
    public class AchievementsScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        GameObject _root;
        // References vers les labels d'etat (DEBLOQUE / VERROUILLE) pour refresh a l'ouverture.
        readonly TextMeshProUGUI[] _stateLabels = new TextMeshProUGUI[4];

        static readonly Color UnlockedGreen = new Color(0.55f, 0.85f, 0.45f);
        static readonly Color LockedGrey    = new Color(0.55f, 0.50f, 0.45f);

        // ----------------------- auto-spawn (idiome MinimapUI) -----------------------
        // Le RuntimeInitialize garantit l'abonnement meme en fast enter play mode.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // anti double-abonnement
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // L'ecran n'a de sens que dans le menu principal.
            if (scene.name != "MainMenuScene") return;
            // Instance deja presente (persistante entre rechargements de scene) : on skip.
            if (FindAnyObjectByType<AchievementsScreen>() != null) return;
            new GameObject("AchievementsUI").AddComponent<AchievementsScreen>();
        }

        // ----------------------- cycle de vie -----------------------

        void Awake()
        {
            Build();
            _root.SetActive(false);
            IsOpen = false;
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (!kb.escapeKey.wasPressedThisFrame) return;

            // Echap ferme l'ecran s'il est ouvert.
            if (IsOpen)
            {
                Close();
                return;
            }

            // Sinon, Echap ouvre les succes uniquement dans le menu principal,
            // et seulement si aucun autre ecran overlay n'est deja ouvert
            // (sinon Echap leur appartient en priorite).
            if (SceneManager.GetActiveScene().name != "MainMenuScene") return;
            if (KeybindsScreen.IsOpen) return;
            if (DiscordBridge.UI.LinkAccountScreen.IsOpen) return;
            if (DiscordBridge.UI.InventoryScreen.IsOpen) return;

            Open();
        }

        // ----------------------- ouverture / fermeture -----------------------

        public void Open()
        {
            // Re-evalue les conditions a chaque ouverture : un succes peut se debloquer
            // pendant que l'ecran est ferme (ex : terminer un niveau puis revenir au menu).
            AchievementStore.EvaluateAll();
            RefreshStates();
            _root.SetActive(true);
            IsOpen = true;
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            IsOpen = false;
        }

        // Met a jour les 4 labels d'etat (texte + couleur) selon le flag PlayerPrefs courant.
        void RefreshStates()
        {
            for (int i = 0; i < AchievementStore.Defs.Length; i++)
            {
                TextMeshProUGUI label = _stateLabels[i];
                if (label == null) continue;

                bool unlocked = AchievementStore.IsUnlocked(AchievementStore.Defs[i].Id);
                label.text  = unlocked ? "DÉBLOQUÉ" : "VERROUILLÉ";
                label.color = unlocked ? UnlockedGreen : LockedGrey;
            }
        }

        // ----------------------- construction de l'UI -----------------------

        void Build()
        {
            EnsureEventSystem();

            // 1. Canvas overlay, sortingOrder 500 (au-dessus des HUD existants).
            _root = new GameObject("AchievementsCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 2. Voile plein ecran (assombrit + absorbe les clics hors pancarte).
            GameObject veilGo = new GameObject("Veil", typeof(RectTransform), typeof(Image));
            veilGo.transform.SetParent(_root.transform, false);
            Image veil = veilGo.GetComponent<Image>();
            UI.PancarteStyle.ApplyVeil(veil);
            Stretch(veil.rectTransform);

            // 3. Planche pancarte centree 1240x675.86.
            GameObject plankGo = new GameObject("Plank", typeof(RectTransform), typeof(Image));
            plankGo.transform.SetParent(_root.transform, false);
            Image plankImg = plankGo.GetComponent<Image>();
            UI.PancarteStyle.ApplyPlank(plankImg);
            RectTransform plankRt = plankImg.rectTransform;
            plankRt.anchorMin = plankRt.anchorMax = plankRt.pivot = new Vector2(0.5f, 0.5f);
            plankRt.anchoredPosition = Vector2.zero;
            plankRt.sizeDelta = new Vector2(1240f, 675.86f);

            // 4. Titre "SUCCES" en haut de la planche (ancre haut, y = -28).
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(plankRt, false);
            TextMeshProUGUI title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "SUCCÈS";
            title.fontSize = 40f;
            title.fontStyle = FontStyles.Bold;
            title.color = UI.PancarteStyle.TextCream;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(0f, 50f);

            // 5. Sous-titre "Echap pour fermer" sous le titre.
            GameObject hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGo.transform.SetParent(plankRt, false);
            TextMeshProUGUI hint = hintGo.GetComponent<TextMeshProUGUI>();
            hint.text = "Echap pour fermer";
            hint.fontSize = 16f;
            hint.color = UI.PancarteStyle.HintSand;
            hint.alignment = TextAlignmentOptions.Center;
            hint.raycastTarget = false;
            RectTransform hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 1f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.anchoredPosition = new Vector2(0f, -76f);
            hintRt.sizeDelta = new Vector2(0f, 24f);

            // 6. Colonne des 4 lignes de succes, centree dans la planche.
            //    VerticalLayoutGroup spacing 14, largeur 800, hauteur de ligne 70.
            GameObject columnGo = new GameObject("Column", typeof(RectTransform));
            columnGo.transform.SetParent(plankRt, false);
            RectTransform columnRt = columnGo.GetComponent<RectTransform>();
            columnRt.anchorMin = columnRt.anchorMax = columnRt.pivot = new Vector2(0.5f, 0.5f);
            columnRt.anchoredPosition = new Vector2(0f, 10f);
            columnRt.sizeDelta = new Vector2(800f, 400f);

            VerticalLayoutGroup layout = columnGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // 7. Les 4 lignes, une par succes, dans l'ordre de Defs.
            for (int i = 0; i < AchievementStore.Defs.Length; i++)
                BuildRow(columnRt, i);

            // 8. Bouton FERMER en bas-centre de la planche.
            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image));
            closeGo.transform.SetParent(plankRt, false);
            Image closeImg = closeGo.GetComponent<Image>();
            closeImg.color = Color.white;
            closeImg.raycastTarget = true;
            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            UI.PancarteStyle.StyleButton(closeBtn, false);
            RectTransform closeRt = closeImg.rectTransform;
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 30f);
            closeRt.sizeDelta = new Vector2(220f, 44f);
            closeBtn.onClick.AddListener(Close);

            TextMeshProUGUI closeLabel = MakeText("Label", closeRt, "FERMER", 22f, FontStyles.Bold);
            Stretch(closeLabel.rectTransform);

            SetLayerRecursively(_root, LayerMask.NameToLayer("UI"));
        }

        // Construit une ligne de succes : fond noir alpha 0.35, titre + description a gauche,
        // etat (DEBLOQUE/VERROUILLE) aligne a droite.
        void BuildRow(RectTransform parent, int index)
        {
            AchievementDef def = AchievementStore.Defs[index];

            GameObject rowGo = new GameObject("Row_" + def.Id, typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(parent, false);

            // Fond de ligne : noir alpha 0.35 (decoratif, ne capte pas les clics).
            Image rowImg = rowGo.GetComponent<Image>();
            rowImg.sprite = null;
            rowImg.color = new Color(0f, 0f, 0f, 0.35f);
            rowImg.raycastTarget = false;

            RectTransform rowRt = rowImg.rectTransform;

            // Le VerticalLayoutGroup pilote la hauteur via le LayoutElement.
            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 70f;
            le.minHeight = 70f;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;

            // Titre du succes : gras 22 TextCream, ancre haut-gauche, x+20.
            TextMeshProUGUI titleText = MakeText("Title", rowRt, def.Title, 22f, FontStyles.Bold);
            titleText.alignment = TextAlignmentOptions.Left;
            RectTransform titleRt = titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0.75f, 1f);
            titleRt.offsetMin = new Vector2(20f, 0f);
            titleRt.offsetMax = Vector2.zero;

            // Description : 16 HintSand, ancre bas-gauche, x+20.
            TextMeshProUGUI descText = MakeText("Desc", rowRt, def.Description, 16f, FontStyles.Normal);
            descText.color = UI.PancarteStyle.HintSand;
            descText.alignment = TextAlignmentOptions.Left;
            RectTransform descRt = descText.rectTransform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(0.75f, 0.5f);
            descRt.offsetMin = new Vector2(20f, 0f);
            descRt.offsetMax = Vector2.zero;

            // Etat : 18 aligne a droite, vert si debloque sinon gris.
            // Le texte et la couleur sont rafraichis a chaque ouverture (RefreshStates).
            TextMeshProUGUI stateText = MakeText("State", rowRt, "VERROUILLÉ", 18f, FontStyles.Bold);
            stateText.color = LockedGrey;
            stateText.alignment = TextAlignmentOptions.Right;
            RectTransform stateRt = stateText.rectTransform;
            stateRt.anchorMin = new Vector2(0.75f, 0f);
            stateRt.anchorMax = new Vector2(1f, 1f);
            stateRt.offsetMin = Vector2.zero;
            stateRt.offsetMax = new Vector2(-20f, 0f);

            _stateLabels[index] = stateText;
        }

        // ----------------------- helpers -----------------------

        static TextMeshProUGUI MakeText(string name, Transform parent, string content, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = UI.PancarteStyle.TextCream;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static void EnsureEventSystem()
        {
            // EventSystem.current peut etre null hors Play : on cherche l'objet lui-meme
            // pour ne pas en dupliquer un (cf. KeybindsScreen).
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            go.transform.SetParent(null);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return; // couche « UI » absente du projet : on ne touche a rien
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }
    }
}
