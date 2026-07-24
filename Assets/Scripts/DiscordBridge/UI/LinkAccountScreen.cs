using DiscordBridge.Controllers;
using DiscordBridge.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Vue pure : ne parle qu'à LinkAccountController (actions) et PlayerProfileData (état,
    // ScriptableObject), jamais à DiscordAPIBridge/SessionStore directement.
    // Ouvert à la demande (LinkAccountMenuButton.Open), affiche l'état "lié" ou "non lié".
    public class LinkAccountScreen : MonoBehaviour
    {
        // Vrai tant que l'ecran est affiche : sert a empecher Echap d'ouvrir les succes
        // par-dessus cet ecran (cf. AchievementsScreen.Update).
        public static bool IsOpen { get; private set; }

        [SerializeField] LinkAccountController controller;
        [SerializeField] PlayerProfileData profileData;

        [Header("Etat NON lie")]
        [SerializeField] GameObject notLinkedGroup;
        [SerializeField] TMP_InputField codeInput;
        [SerializeField] Button submitButton;

        [Header("Etat LIE")]
        [SerializeField] GameObject linkedGroup;
        [SerializeField] TextMeshProUGUI linkedInfoText;
        [SerializeField] Button unlinkButton;

        [Header("Commun")]
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button closeButton;

        // Invitation officielle : bouton permanent, visible dans les 2 etats (lie ou non).
        const string DiscordInviteUrl = "https://discord.gg/qAtH7XuHc";

        [Header("Animation (optionnel)")]
        [SerializeField] UIPanelAnimator panelAnimator;

        [Header("Skin pancarte (facon ecran Touches)")]
        [Tooltip("Pancarte pixel art de fond. Vide = chargee depuis Resources/UI/pencarte.")]
        [SerializeField] Sprite plankSprite;
        [Tooltip("Applique le relooking pancarte au premier affichage.")]
        [SerializeField] bool pancarteSkin = true;

        bool _skinned;

        void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
            if (unlinkButton != null) unlinkButton.onClick.AddListener(OnUnlinkClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            if (submitButton == null)
                Debug.LogWarning("[DiscordBridge] LinkAccountScreen : aucun bouton de validation assigné.");

            ApplyPancarteSkin();
        }

        void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitClicked);
            if (unlinkButton != null) unlinkButton.onClick.RemoveListener(OnUnlinkClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        void OnEnable()
        {
            if (controller == null)
            {
                Debug.LogWarning("[DiscordBridge] LinkAccountScreen : aucun LinkAccountController assigné.");
                return;
            }

            controller.OnLinkSucceeded += HandleLinkSucceeded;
            controller.OnLinkFailed += HandleLinkFailed;
            controller.OnUnlinked += HandleUnlinked;
            if (profileData != null) profileData.OnProfileUpdated += Refresh;

            SetStatus(string.Empty);
            SetInteractable(true);
            Refresh();
        }

        void OnDisable()
        {
            if (profileData != null) profileData.OnProfileUpdated -= Refresh;
            if (controller == null) return;

            controller.OnLinkSucceeded -= HandleLinkSucceeded;
            controller.OnLinkFailed -= HandleLinkFailed;
            controller.OnUnlinked -= HandleUnlinked;
        }

        // Ouvre l'ecran depuis un bouton du menu.
        public void Open()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (panelAnimator != null) panelAnimator.PlayOpen();
        }

        void Close()
        {
            if (panelAnimator != null)
                panelAnimator.PlayClose(() =>
                {
                    gameObject.SetActive(false);
                    IsOpen = false;
                });
            else
            {
                gameObject.SetActive(false);
                IsOpen = false;
            }
        }

        // Affiche le bon groupe selon l'etat de liaison courant.
        void Refresh()
        {
            bool linked = controller != null && controller.IsLinked;
            if (notLinkedGroup != null) notLinkedGroup.SetActive(!linked);
            if (linkedGroup != null) linkedGroup.SetActive(linked);
            if (linked && linkedInfoText != null)
            {
                linkedInfoText.text = profileData != null && profileData.IsLoaded
                    ? $"Compte lié   —   Mana : {profileData.Mana}"
                    : "Compte lié";
            }
        }

        void OnSubmitClicked()
        {
            if (controller == null)
            {
                SetStatus("Erreur de configuration (contrôleur manquant).");
                return;
            }

            if (codeInput == null)
            {
                SetStatus("Erreur de configuration (champ de saisie manquant).");
                return;
            }

            if (!int.TryParse(codeInput.text, out int code) || code < 1000 || code > 9999)
            {
                SetStatus("Code invalide : entre 4 chiffres.");
                return;
            }

            SetStatus("Connexion en cours...", interactable: false);
            _ = controller.SubmitCodeAsync(code);
        }

        void OnUnlinkClicked()
        {
            if (controller != null)
                controller.Unlink();
        }

        void HandleLinkSucceeded()
        {
            SetStatus("Compte lié !");
            Refresh();
        }

        void HandleLinkFailed(string errorMessage) => SetStatus($"Erreur : {errorMessage}", interactable: true);

        void HandleUnlinked()
        {
            if (codeInput != null) codeInput.text = string.Empty;
            SetStatus("Compte délié.");
            Refresh();
        }

        void SetStatus(string message, bool interactable = true)
        {
            if (statusText != null) statusText.text = message;
            SetInteractable(interactable);
        }

        void SetInteractable(bool interactable)
        {
            if (submitButton != null) submitButton.interactable = interactable;
            if (codeInput != null) codeInput.interactable = interactable;
        }

        // ----------------------- skin « pancarte » (facon ecran Touches) -----------------------
        // Relooking 100 % runtime au premier affichage : reparente le contenu existant
        // (champs serialises, jamais detruits) sous une pancarte pixel art. Aucune
        // reference Networking/DTOs : la vue ne restyle qu'elle-meme.

        static readonly Color TextCream = new Color32(0xF5, 0xE6, 0xC8, 0xFF);
        static readonly Color HintSand = new Color32(0xD2, 0xBC, 0x99, 0xFF);
        static readonly Color WoodNormal = new Color32(0x3A, 0x28, 0x1C, 0xFF);
        static readonly Color WoodHover = new Color32(0x5C, 0x3F, 0x2A, 0xFF);
        static readonly Color RedNormal = new Color32(0x5A, 0x23, 0x20, 0xFF);
        static readonly Color RedHover = new Color32(0x7A, 0x32, 0x2C, 0xFF);
        static readonly Color InputNormal = new Color32(0x2A, 0x1C, 0x12, 0xFF);
        static readonly Color InputHover = new Color32(0x3C, 0x2A, 0x1C, 0xFF);
        static readonly Color StatusAmber = new Color32(0xFF, 0xC2, 0x4D, 0xFF);
        static readonly Color DimVeil = new Color(0f, 0f, 0f, 0.55f);

        const string PlankResourcePath = "UI/pencarte";

        void ApplyPancarteSkin()
        {
            if (_skinned || !pancarteSkin) return;
            _skinned = true;

            Sprite sprite = plankSprite;
            if (sprite == null) sprite = Resources.Load<Sprite>(PlankResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning("[DiscordBridge] LinkAccountScreen : sprite " + PlankResourcePath +
                                 " introuvable — skin pancarte ignore.");
                return;
            }

            // 1. Voile plein ecran : assombrit le menu derriere et absorbe les clics.
            Image veil = GetComponent<Image>();
            if (veil != null)
            {
                veil.sprite = null;
                veil.color = DimVeil;
                veil.raycastTarget = true;
            }

            // Le titre n'est pas un champ serialise : on le retrouve par son nom.
            Transform title = transform.Find("Title");

            // 2. La pancarte en fond, derriere tout le contenu.
            var plankGo = new GameObject("Plank", typeof(RectTransform), typeof(Image));
            RectTransform plank = (RectTransform)plankGo.transform;
            plank.SetParent(transform, false);
            plank.SetAsFirstSibling();
            Image plankImage = plankGo.GetComponent<Image>();
            plankImage.sprite = sprite;
            plankImage.color = Color.white;
            plankImage.preserveAspect = true;
            plankImage.raycastTarget = false;
            plank.anchorMin = plank.anchorMax = plank.pivot = new Vector2(0.5f, 0.5f);
            plank.anchoredPosition = Vector2.zero;
            float ratio = sprite.rect.height / sprite.rect.width;
            plank.sizeDelta = new Vector2(1240f, 1240f * ratio);

            // 3. Zone de bois utile dans le sprite (le cadre et les chaines restent vides).
            var boardGo = new GameObject("Board", typeof(RectTransform));
            RectTransform board = (RectTransform)boardGo.transform;
            board.SetParent(plank, false);
            board.anchorMin = new Vector2(0.243f, 0.168f);
            board.anchorMax = new Vector2(0.757f, 0.727f);
            board.offsetMin = Vector2.zero;
            board.offsetMax = Vector2.zero;
            VerticalLayoutGroup column = boardGo.AddComponent<VerticalLayoutGroup>();
            column.spacing = 8f;
            column.padding = new RectOffset(0, 0, 4, 4);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.MiddleCenter;

            // 4. Tout le contenu existant migre dans la planche, dans l'ordre.
            if (title != null) title.SetParent(board, false);
            if (notLinkedGroup != null) notLinkedGroup.transform.SetParent(board, false);
            if (linkedGroup != null) linkedGroup.transform.SetParent(board, false);
            if (statusText != null) statusText.transform.SetParent(board, false);
            if (closeButton != null) closeButton.transform.SetParent(board, false);

            // 5. Tailles et styles.
            if (title != null)
            {
                SetLayout(title.gameObject, 620f, 46f);
                TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = "COMPTE DISCORD";
                    titleText.fontSize = 34f;
                    titleText.fontStyle = FontStyles.Bold;
                    titleText.color = TextCream;
                    titleText.alignment = TextAlignmentOptions.Center;
                    titleText.raycastTarget = false;
                }
            }

            if (notLinkedGroup != null)
            {
                SetLayout(notLinkedGroup, 620f, 168f);
                EnsureColumn(notLinkedGroup, 12f);

                // L'aide vit DANS notLinkedGroup : Refresh() la masque tout seul
                // quand le compte est deja lie.
                var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
                hintGo.transform.SetParent(notLinkedGroup.transform, false);
                hintGo.transform.SetAsFirstSibling();
                TextMeshProUGUI hint = hintGo.GetComponent<TextMeshProUGUI>();
                hint.text = "Tape /lier sur le Discord, puis entre ici le code à 4 chiffres.";
                hint.fontSize = 19f;
                hint.color = HintSand;
                hint.alignment = TextAlignmentOptions.Center;
                hint.raycastTarget = false;
                SetLayout(hintGo, 600f, 46f);

                if (codeInput != null)
                {
                    SetLayout(codeInput.gameObject, 300f, 52f);
                    RestyleInput(codeInput);
                }
                if (submitButton != null)
                {
                    SetLayout(submitButton.gameObject, 240f, 46f);
                    RestyleButton(submitButton, WoodNormal, WoodHover);
                }
            }

            if (linkedGroup != null)
            {
                SetLayout(linkedGroup, 620f, 116f);
                EnsureColumn(linkedGroup, 12f);
                if (linkedInfoText != null)
                {
                    SetLayout(linkedInfoText.gameObject, 600f, 44f);
                    linkedInfoText.fontSize = 24f;
                    linkedInfoText.color = TextCream;
                    linkedInfoText.alignment = TextAlignmentOptions.Center;
                    linkedInfoText.raycastTarget = false;
                }
                if (unlinkButton != null)
                {
                    SetLayout(unlinkButton.gameObject, 240f, 46f);
                    RestyleButton(unlinkButton, RedNormal, RedHover);
                }
            }

            if (statusText != null)
            {
                SetLayout(statusText.gameObject, 620f, 34f);
                statusText.fontSize = 21f;
                statusText.color = StatusAmber;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.raycastTarget = false;
            }

            if (closeButton != null)
            {
                SetLayout(closeButton.gameObject, 220f, 44f);
                RestyleButton(closeButton, WoodNormal, WoodHover);
            }

            // Bouton d'invitation au serveur : visible dans les 2 etats,
            // insere juste au-dessus de FERMER dans la colonne.
            if (closeButton != null)
            {
                var joinGo = new GameObject("JoinDiscordButton", typeof(RectTransform), typeof(Image), typeof(Button));
                joinGo.transform.SetParent(board, false);
                joinGo.transform.SetSiblingIndex(closeButton.transform.GetSiblingIndex());
                var joinLblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                joinLblGo.transform.SetParent(joinGo.transform, false);
                joinLblGo.GetComponent<TextMeshProUGUI>().text = "REJOINDRE LE DISCORD";
                Button joinBtn = joinGo.GetComponent<Button>();
                RestyleButton(joinBtn, WoodNormal, WoodHover);
                joinBtn.onClick.AddListener(() => Application.OpenURL(DiscordInviteUrl));
                SetLayout(joinGo, 340f, 44f);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(board);
        }

        static void SetLayout(GameObject go, float width, float height)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
        }

        static void EnsureColumn(GameObject group, float spacing)
        {
            VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = group.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }

        static void RestyleButton(Button button, Color normal, Color hover)
        {
            // Le teintage d'un Button multiplie la couleur de son Image :
            // image en blanc, la vraie couleur va dans le ColorBlock.
            Image background = button.targetGraphic as Image;
            if (background != null) background.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = hover;
            colors.pressedColor = new Color(hover.r * 0.8f, hover.g * 0.8f, hover.b * 0.8f, 1f);
            colors.selectedColor = normal;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.5f);
            button.colors = colors;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = TextCream;
                label.fontSize = 22f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                RectTransform rect = label.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        static void RestyleInput(TMP_InputField input)
        {
            Image background = input.targetGraphic as Image;
            if (background != null) background.color = Color.white;

            ColorBlock colors = input.colors;
            colors.normalColor = InputNormal;
            colors.highlightedColor = InputHover;
            colors.pressedColor = new Color(InputHover.r * 0.8f, InputHover.g * 0.8f, InputHover.b * 0.8f, 1f);
            colors.selectedColor = InputNormal;
            colors.disabledColor = new Color(InputNormal.r, InputNormal.g, InputNormal.b, 0.5f);
            input.colors = colors;

            if (input.textComponent != null)
            {
                input.textComponent.color = TextCream;
                input.textComponent.fontSize = 28f;
                input.textComponent.alignment = TextAlignmentOptions.Center;
            }

            TextMeshProUGUI placeholder = input.placeholder as TextMeshProUGUI;
            if (placeholder != null)
            {
                Color sand = HintSand;
                sand.a = 0.55f;
                placeholder.color = sand;
                placeholder.fontSize = 20f;
                placeholder.alignment = TextAlignmentOptions.Center;
            }
        }
    }
}
