using Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menus
{
    public class LevelEndMenuManager : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] GameObject nextLevelButton;

        private void Start()
        {
            panel.SetActive(false);
        }

        public void Show(bool won)
        {
            panel.SetActive(true);
            titleText.text = won ? "Victory!" : "Defeat";
            messageText.text = won ? "All waves cleared, yay!" : "Oh no, your village was destroyed...";

            if (nextLevelButton)
            {
                LevelData current = LevelSelectManager.SelectedLevel;
                bool isFinalLevel = !current || current.levelID >= 7;
                nextLevelButton.SetActive(won && !isFinalLevel);
            }
        }

        // TODO change up buttons: if win maybe no point in trying level again?
        public void OnTryAgain()
        {
            print("Restarting level..."); // TODO display number 4 debug purposes
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnQuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenuScene");
        }

        public void OnQuitToLevelSelect()
        {
            Time.timeScale = 1f;
            StartMenuManager.OpenLevelSelectOnStart = true;
            SceneManager.LoadScene("MainMenuScene");
        }

        public void OnNextLevel()
        {
            LevelData current = LevelSelectManager.SelectedLevel;
            if (current == null) return;

            // find by levelID
            LevelData[] allLevels = Resources.FindObjectsOfTypeAll<LevelData>();
            LevelData next = null;
            foreach (LevelData ld in allLevels)
                if (ld.levelID == current.levelID + 1)
                {
                    next = ld;
                    break;
                }

            if (next == null) return;

            LevelSelectManager.SelectNextLevel(next);
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameScene");
        }
    }
}