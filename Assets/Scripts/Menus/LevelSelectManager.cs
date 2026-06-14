using UnityEngine;
using Game;
using UnityEngine.SceneManagement;

namespace Menus
{
    public class LevelSelectManager : MonoBehaviour
    {
        public static LevelSelectManager Instance { get; private set; }
        public static LevelData SelectedLevel { get; private set; }

        [Header("Level Buttons (assign in order 1–8)")] [SerializeField]
        LevelButtonUI[] levelButtons;

        [Header("Scene names")] [SerializeField]
        string placementSceneName = "GameScene";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnEnable() => RefreshButtons();

        public void RefreshButtons()
        {
            foreach (var btn in levelButtons)
                btn.Refresh();
        }

        public void SelectLevel(LevelData data)
        {
            SelectedLevel = data;
            SceneManager.LoadScene(placementSceneName);
        }

        public static void SelectNextLevel(LevelData data)
        {
            SelectedLevel = data;
        }
    }
}