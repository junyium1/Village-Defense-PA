using Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menus
{
    /// <summary>
    /// Planche de sélection de niveau (menu 3D).
    /// Affiche l'état du niveau (verrouillé / terminé / disponible) et charge la scène de jeu au clic.
    /// Réutilise la logique existante : <see cref="Player.IsLevelUnlocked"/>,
    /// <see cref="LevelSelectManager.SelectNextLevel"/>.
    /// </summary>
    public class SignLevelPlank : SignPlankBase
    {
        static readonly Color LockedColor = new Color(0.45f, 0.42f, 0.38f);
        static readonly Color CompletedColor = new Color(0.98f, 0.80f, 0.35f);
        static readonly Color AvailableColor = new Color(0.96f, 0.92f, 0.82f);

        [SerializeField] LevelData levelData;
        [SerializeField] TextMeshPro label;
        [SerializeField] string gameSceneName = "GameScene";

        bool _unlocked;

        void OnEnable() => Refresh();

        /// <summary>Met à jour le libellé et la couleur selon la progression du joueur.</summary>
        public void Refresh()
        {
            if (levelData == null || label == null) return;

            _unlocked = Player.Instance != null && Player.Instance.IsLevelUnlocked(levelData.levelID);
            bool completed = Player.Instance != null && Player.Instance.IsLevelCompleted(levelData.levelID);

            label.text = levelData.isBoss ? "[BOSS] " + levelData.levelName : levelData.levelName;
            label.color = !_unlocked ? LockedColor : (completed ? CompletedColor : AvailableColor);
        }

        public override void OnClicked()
        {
            if (levelData == null || !_unlocked) return;

            LevelSelectManager.SelectNextLevel(levelData);
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
