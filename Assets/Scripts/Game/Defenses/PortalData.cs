using UnityEngine;

namespace Game.Defenses
{
    [CreateAssetMenu(menuName = "Defense/Portal")]
    public class PortalData : Defenses.DefenseData
    {
        public int capacity; // nb of enemies before despawning
        public float triggerRadius;
    }
}