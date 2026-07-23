using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menus
{
    public class StartMenuManager : MonoBehaviour
    {
        public static bool OpenLevelSelectOnStart = false;

        [Header("Menus")]
        public GameObject mainMenuPanel;
        public GameObject optionsMenuPanel;
        public GameObject levelMenuPanel;

        [Header("Menu 3D")]
        [Tooltip("Si coché, les panels 2D (main/options/level) restent fermés : le menu 3D prend le relais.")]
        public bool useMenu3D = false;

        // ----------------------- main menu -----------------------
        void Start()
        {
            levelMenuPanel.SetActive(false);
            if (useMenu3D)
            {
                mainMenuPanel.SetActive(false);
                optionsMenuPanel.SetActive(false);
                return;
            }
            mainMenuPanel.SetActive(true);
            if (OpenLevelSelectOnStart)
            {
                OpenLevelSelectOnStart = false;
                GoToLevelSelect();
            }
        }
        
        public void GoToLevelSelect()
        {
            mainMenuPanel.SetActive(false);
            levelMenuPanel.SetActive(true);
            LevelSelectManager.Instance.RefreshButtons();
        }
        
        public void GoToMainMenu()
        {
            levelMenuPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            Application.Quit();
        }
        
        public void WipeSave()
        {
            Player.Instance.ResetProgress(); // efface progression + niveaux d'upgrade du shop
            // Un wipe de save reinitialise aussi les succes (coherence avec la progression).
            Game.AchievementStore.ResetAll();
            if (LevelSelectManager.Instance != null)
                LevelSelectManager.Instance.RefreshButtons();
        }
        
        // ----------------------- settings menu -----------------------
        public void OpenOptions()
        {
            mainMenuPanel.SetActive(false);
            optionsMenuPanel.SetActive(true);
        }
        
        public void CloseOptions()
        {
            optionsMenuPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }
}