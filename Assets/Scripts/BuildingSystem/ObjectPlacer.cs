using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] List<GameObject> placedGameObjects = new();

    public int PlaceObject(GameObject prefab, Vector3 position)
    {
        GameObject newBuilding = Instantiate(prefab);
        newBuilding.transform.position = position;
        placedGameObjects.Add(newBuilding);
        return placedGameObjects.Count - 1;
    }

    public GameObject GetPlacedObject(int index)
    {
        if (index < 0 || index >= placedGameObjects.Count) return null;
        return placedGameObjects[index];
    }

    internal void RemoveObjectAt(int gameObjectIndex)
    {
        if (placedGameObjects.Count <= gameObjectIndex || placedGameObjects[gameObjectIndex] == null)
        {
            return;
        }

        Destroy(placedGameObjects[gameObjectIndex]);
        placedGameObjects[gameObjectIndex] = null;
    }
}
