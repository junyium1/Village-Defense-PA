using UnityEngine;

namespace Game
{
    /// <summary>Catalogue unique des niveaux : tous les LevelData de Resources/Levels,
    /// triés par levelID. Indépendant des managers de scène — disponible partout,
    /// tout le temps (contrairement à LevelSelectManager, dont le panel 2D est désactivé).</summary>
    public static class LevelCatalog
    {
        static LevelData[] _cache;

        /// <summary>Tous les niveaux connus, triés par levelID croissant (jamais null).</summary>
        public static LevelData[] GetAll()
        {
            if (_cache == null)
            {
                _cache = Resources.LoadAll<LevelData>("Levels");
                System.Array.Sort(_cache, (a, b) => a.levelID.CompareTo(b.levelID));
            }
            return _cache;
        }
    }
}
