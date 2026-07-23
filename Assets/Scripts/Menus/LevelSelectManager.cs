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

        // Collecte les LevelData des boutons non null, tries par levelID croissant.
        // Sert aux succes : securise l'ordre meme si le tableau Inspector diverge.
        public LevelData[] GetOrderedLevels()
        {
            System.Collections.Generic.List<LevelData> list =
                new System.Collections.Generic.List<LevelData>(levelButtons != null ? levelButtons.Length : 0);

            if (levelButtons != null)
            {
                foreach (LevelButtonUI btn in levelButtons)
                {
                    if (btn == null) continue;
                    LevelData data = btn.Data;
                    if (data == null) continue;
                    list.Add(data);
                }
            }

            list.Sort((a, b) => a.levelID.CompareTo(b.levelID));
            return list.ToArray();
        }
    }
}