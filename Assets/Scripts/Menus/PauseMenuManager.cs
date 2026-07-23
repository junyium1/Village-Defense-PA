using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menus
{
    public class PauseMenuManager : MonoBehaviour
    {
        public GameObject pauseMenuPanel;
        private bool _isPaused;
        public GameManager gameManager;

        [Tooltip("Menu pause 3D (pancarte). Si câblé, il remplace le panel 2D\n" +
                 "(le panel reste en secours quand le 3D est absent).")]
        public PauseMenu3DController pauseMenu3D;

        private void Start()
        {
            pauseMenuPanel.SetActive(false);
        }

        // once per frame
        private void Update()
        {
            // L'écran Touches consomme Echap (fermeture / annulation d'un rebind) :
            // sans ce garde, reprendre le jeu passerait avant sa fermeture.
            if (Menus.KeybindsScreen.IsOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Priorité : si un panneau ouvert via PanelToggle (ex: upgrade) est affiché
                // et qu'on n'est pas en pause, Échap le ferme d'abord sans ouvrir la pause.
                if (!_isPaused && PanelToggle.OpenPanel != null && PanelToggle.OpenPanel.activeSelf)
                {
                    PanelToggle.OpenPanel.SetActive(false);
                    return;
                }

                if (_isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Resume()
        {
            if (pauseMenu3D != null) pauseMenu3D.Hide();
            else pauseMenuPanel.SetActive(false);
            gameManager.ResumeGame();
            _isPaused = false;
        }

        private void Pause()
        {
            if (pauseMenu3D != null) pauseMenu3D.Show();
            else pauseMenuPanel.SetActive(true);
            gameManager.PauseGame();
            _isPaused = true;
        }

        public void QuitToMainMenu()
        {
            Resume();
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}