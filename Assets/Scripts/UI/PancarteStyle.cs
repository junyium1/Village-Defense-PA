using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Helper statique reutilisable : palette « pancarte bois » + chargement du sprite.
    // Centralise les valeurs pour garantir la coherence visuelle entre tous les ecrans
    // construits par code (KeybindsScreen, LinkAccountScreen, AchievementsScreen, etc.).
    // Les couleurs sont copiees de LinkAccountScreen (~lignes 180-189).
    public static class PancarteStyle
    {
        public static readonly Color32 TextCream  = new Color32(0xF5, 0xE6, 0xC8, 0xFF);
        public static readonly Color32 HintSand   = new Color32(0xD2, 0xBC, 0x99, 0xFF);
        public static readonly Color32 WoodNormal = new Color32(0x3A, 0x28, 0x1C, 0xFF);
        public static readonly Color32 WoodHover  = new Color32(0x5C, 0x3F, 0x2A, 0xFF);
        public static readonly Color32 RedNormal  = new Color32(0x5A, 0x23, 0x20, 0xFF);
        public static readonly Color32 RedHover   = new Color32(0x7A, 0x32, 0x2C, 0xFF);

        const string PlankResourcePath = "UI/pencarte";

        // Charge le sprite pancarte depuis Resources (null-safe : retourne null si absent).
        public static Sprite LoadPlank()
        {
            return Resources.Load<Sprite>(PlankResourcePath);
        }

        // Applique un voile noir alpha 0.55 (assombrit l'arriere-plan et absorbe les clics).
        public static void ApplyVeil(Image img)
        {
            if (img == null) return;
            img.sprite = null;
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = true;
        }

        // Applique le sprite pancarte (Simple, preserveAspect).
        // Si le sprite est introuvable, l'image reste sans sprite (aplat blanc).
        public static void ApplyPlank(Image img)
        {
            if (img == null) return;
            Sprite sprite = LoadPlank();
            if (sprite == null)
                Debug.LogWarning("[PancarteStyle] Sprite pancarte introuvable (Resources/" +
                                 PlankResourcePath + ") — fond sans texture.");
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;
        }

        // Configure le ColorBlock d'un bouton : bois (red=false) ou rouge (red=true).
        // L'image du bouton doit rester blanche : la couleur passe par le teintage ColorBlock.
        public static void StyleButton(Button btn, bool red)
        {
            if (btn == null) return;

            Color32 normal = red ? RedNormal : WoodNormal;
            Color32 hover  = red ? RedHover  : WoodHover;
            // Le pressed assombrit le hover (x0.8) pour un retour tactile visible.
            Color32 pressed = new Color32(
                (byte)(hover.r * 0.8f),
                (byte)(hover.g * 0.8f),
                (byte)(hover.b * 0.8f),
                0xFF
            );

            Image background = btn.targetGraphic as Image;
            if (background != null) background.color = Color.white;

            ColorBlock colors = btn.colors;
            colors.normalColor    = normal;
            colors.highlightedColor = hover;
            colors.pressedColor   = pressed;
            colors.selectedColor  = normal;
            colors.disabledColor  = new Color(normal.r, normal.g, normal.b, 0.5f);
            colors.fadeDuration   = 0.08f;
            btn.colors = colors;
        }
    }
}
