using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu]
public class BuildingsDatabaseSO : ScriptableObject
{
    public List<BuildingData> buildingsData;
}

[Serializable]
public class BuildingData
{
    [field: SerializeField]
    public string Name { get; private set; }
    [field: SerializeField]
    public int ID { get; private set; }
    [field: SerializeField]
    public Vector2Int Size { get; private set; } = Vector2Int.one;
    [field: SerializeField]
    public GameObject Prefab { get; private set; }
}