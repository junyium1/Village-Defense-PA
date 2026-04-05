using UnityEngine;

namespace Game.Defenses
{
    /**
     * Has all the base stats of defensive elements that can be placed down by player.
     * Portals / Traps / Turrets have their own separate mechanisms, walls are just walls.
     * Find defense type-specific information in their respective files.
     */
    public class Defenses
    {
        public abstract class DefenseData : ScriptableObject
        {
            public string displayName;
            public int cost;
            public float maxHp;
            public GameObject prefab;
            // TODO for Ana / Yanis to implement during fluff time lol
            // public Sprite     icon;
        }
    }
}