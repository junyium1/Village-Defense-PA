using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MenusEditor
{
    /// <summary>
    /// Force le mode Play à démarrer sur l'écran-titre (MainMenuScene), quelle que
    /// soit la scène ouverte dans l'éditeur — sinon un Play depuis GameScene saute
    /// le menu principal. Togglable via le menu « Tools » (coché = actif).
    /// Réglage éditeur uniquement (EditorPrefs), non embarqué dans le build.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        const string Pref = "PlayFromMainMenu.Enabled";
        const string MenuPath = "Tools/Lancer le Play depuis le Menu Principal";
        const string MainScenePath = "Assets/Scenes/MainMenuScene.unity";

        static PlayFromMainMenu()
        {
            // On applique une fois que l'AssetDatabase est prête (après domain reload) :
            // un simple delayCall peut tomber pendant l'import et charger un asset null.
            EditorApplication.update += ApplyWhenReady;
        }

        static bool Enabled
        {
            get => EditorPrefs.GetBool(Pref, true);
            set => EditorPrefs.SetBool(Pref, value);
        }

        static void ApplyWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= ApplyWhenReady;
            Apply();
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath)
                : null;
        }
    }
}
