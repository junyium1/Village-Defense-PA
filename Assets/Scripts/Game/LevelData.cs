using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Levels/LevelData")]
    public class LevelData : ScriptableObject
    {
        public string levelName;
        public int levelID;
        public int maxLives;
        public int totalWaves;
        public float timeBetweenWaves;
        public int goldReward;
        public int maxCrystalsReward;
        public bool isBoss;
    }
}