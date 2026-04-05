using UnityEngine;

namespace Game
{
    //TODO actually make differnt levels
    [CreateAssetMenu(menuName = "Levels/LevelData")]
    public class LevelData : ScriptableObject
    {
        public string levelName;
        public int maxLives;
        public int totalWaves;
        public float timeBetweenWaves;
        public int goldReward;
        public int maxCrystalsReward;
    }
}