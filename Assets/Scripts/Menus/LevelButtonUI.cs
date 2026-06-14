using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

namespace Menus
{
    public class LevelButtonUI : MonoBehaviour
    {
        [Header("Data")] [SerializeField] LevelData levelData;

        [Header("References")] [SerializeField]
        Button button;

        [SerializeField] TextMeshProUGUI labelText;

        [Header("Optional visuals")] [SerializeField]
        GameObject lockIcon;

        [SerializeField] GameObject completedIcon;
        // TODO make boss indicator sprite
        // [SerializeField] GameObject bossIndicator;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (levelData == null || Player.Instance == null)
                return;

            bool unlocked = Player.Instance.IsLevelUnlocked(levelData.levelID);
            bool completed = Player.Instance.IsLevelCompleted(levelData.levelID);

            if (labelText != null)
                labelText.text = levelData.isBoss
                    ? $"[BOSS] {levelData.levelName}"
                    : levelData.levelName;

            button.interactable = unlocked;

            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (completedIcon != null) completedIcon.SetActive(completed);
            // if (bossIndicator != null) bossIndicator.SetActive(levelData.isBoss);
        }

        public void OnClick()
        {
            if (Player.Instance == null || !Player.Instance.IsLevelUnlocked(levelData.levelID))
                return;

            LevelSelectManager.Instance.SelectLevel(levelData);
        }
    }
}