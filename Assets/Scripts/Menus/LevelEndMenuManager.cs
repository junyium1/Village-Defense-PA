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
        
        [SerializeField] TextMeshProUGUI starsText;
        [SerializeField] TextMeshProUGUI goldText;
        [SerializeField] TextMeshProUGUI crystalsText;

        private void Start()
        {
            panel.SetActive(false);
        }

        public void Show(bool won)
        {
            panel.SetActive(true);
            titleText.text = won ? "Victory!" : "Defeat";
            messageText.text = won ? "All waves cleared, yay!" : "Oh no, your village was destroyed...";

            LevelData current = LevelSelectManager.SelectedLevel;
            CombatManager combat = CombatManager.Instance;

            int stars = 0;
            int goldEarned = 0;
            int crystalsEarned = 0;
            
            if (combat && current)
            {
                stars = CalculateStars(won, combat.playerLives, current.maxLives);
                if (won)
                {
                    goldEarned = combat.GoldEarned;
                    crystalsEarned = combat.CrystalsEarned;
                }
            }
            
            if (starsText) starsText.text = BuildStarString(stars);
            if (goldText) goldText.text = $"+{goldEarned}";
            if (crystalsText) crystalsText.text = $"+{crystalsEarned}";
            
            if (nextLevelButton)
            {
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

        int CalculateStars(bool won, int lives, int maxLives)
        {
            if (!won) return 0;
            if (lives >= maxLives) return 3;
            if (lives > maxLives / 2f) return 2;
            return 1;
        }

        string BuildStarString(int stars)
        {
            return new string('*', stars) + new string('-', 3 - stars);
        }
    }
}