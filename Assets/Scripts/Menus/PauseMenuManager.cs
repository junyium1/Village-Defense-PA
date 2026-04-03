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
    
        private void Start()
        {
            pauseMenuPanel.SetActive(false);
        }

        // once per frame
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
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
            pauseMenuPanel.SetActive(false);
            gameManager.ResumeGame();
            _isPaused = false;
        }

        private void Pause()
        {
            pauseMenuPanel.SetActive(true);
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