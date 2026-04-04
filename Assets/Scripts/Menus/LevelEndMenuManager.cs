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

        private void Start()
        {
            panel.SetActive(false);
        }
        public void Show(bool won)
        {
            panel.SetActive(true);
            titleText.text   = won ? "Victory!"  : "Defeat";
            messageText.text = won ? "All waves cleared, yay!" : "Oh no, your village was destroyed...";
        }

        // TODO change up buttons: if win maybe no point in trying level again?
        public void OnTryAgain()
        {
            print("Restarting level..."); // TODO add a number (id) later when we have multiple levels
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnQuitToMainMenu()
        {
            SceneManager.LoadScene("MainMenuScene");
        }

        // TODO implement level map later
        public void OnQuitToLevelSelect()
        {
            print(">>>>> Quitting to level select - not implemented yet.");
        }
    }
}