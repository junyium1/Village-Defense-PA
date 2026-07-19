using UnityEngine;

namespace Game.Units
{
    // dynamic ish creation, specify values in inspector
    // NOTE: doit vivre dans son propre fichier (nom de classe = nom de fichier), sinon
    // Unity ne peut pas lier les .asset à ce script (m_Script fileID 0 = assets cassés).
    [CreateAssetMenu(menuName = "Units/UnitData")]
    public class UnitData : ScriptableObject
    {
        public string unitName;
        public Faction faction;
        public float maxHp, damage, moveSpeed, attackRange, attackRate;

        // ranged units specific
        public GameObject bulletPrefab;
        public float bulletSpeed;
    }
}
