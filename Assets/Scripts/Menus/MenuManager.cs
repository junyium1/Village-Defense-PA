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
    
        // ----------------------- main menu -----------------------
        void Start()
        {
            levelMenuPanel.SetActive(false);
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
            Player.Instance.ResetProgress();
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