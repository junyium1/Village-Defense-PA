using System;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    private Dictionary<Vector3Int, PlacementData> placedBuildings = new();
    
    public void AddBuilding(Vector3Int gridPosition, 
        Vector2Int buildingSize, 
        int ID, 
        int placedBuildingIndex)
    {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, buildingSize);
        PlacementData data = new PlacementData(positionToOccupy, ID,  placedBuildingIndex);
        foreach (var position in positionToOccupy)
        {
            if (placedBuildings.ContainsKey(position))
            {
                throw new Exception($"Already a building in this cell position {position}");
            }
            placedBuildings[position] = data;
        }
    }

    private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, Vector2Int buildingSize)
    {
        List<Vector3Int> returnValue = new();
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                returnValue.Add(gridPosition + new Vector3Int(x, 0, y));
            }
        }
        return returnValue;
    }

    public bool CanPlaceBuilding(Vector3Int gridPosition, Vector2Int buildingSize)
    {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, buildingSize);
        foreach (var position in positionToOccupy)
        {
            if (placedBuildings.ContainsKey(position))
            {
                return false;
            }
        }
        return true;
    }
}

public class PlacementData
{
    public List<Vector3Int> occupiedPositions;
    public int ID { get; private set; }
    public int PlacedBuildingIndex {get; private set;}

    public PlacementData(List<Vector3Int> occupiedPositions, int iD, int placedBuildingIndex)
    {
        this.occupiedPositions = occupiedPositions;
        this.ID = ID;
        this.PlacedBuildingIndex = PlacedBuildingIndex;
    }
}