using System;
using UnityEngine;

public class PlacementState : IBuildingState
{
    private int selectedBuildingIndex = -1;
    int ID;
    Grid grid;
    PreviewSystem previewSystem;
    BuildingsDatabaseSO database;
    GridData floorData;
    GridData buildingData;
    ObjectPlacer objectPlacer;
    AudioSource placementSound;
    Vector2Int gridSize;

    private GameObject directPrefab;
    private Vector2Int directSize;
    private bool useDirectMode;

    public event Action<GameObject> OnPlaced;

    public PlacementState(int id, 
        Grid grid, 
        PreviewSystem previewSystem, 
        BuildingsDatabaseSO database, 
        GridData floorData, 
        GridData buildingData, 
        ObjectPlacer objectPlacer,
        AudioSource placementSound,
        Vector2Int gridSize)
    {
        ID = id;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.database = database;
        this.floorData = floorData;
        this.buildingData = buildingData;
        this.objectPlacer = objectPlacer;
        this.placementSound = placementSound;
        this.gridSize = gridSize;
        
        selectedBuildingIndex = database.buildingsData.FindIndex(data => data.ID == ID);
        if (selectedBuildingIndex > -1)
        {
            previewSystem.StartShowingPlacementPreview(
                database.buildingsData[selectedBuildingIndex].Prefab, 
                database.buildingsData[selectedBuildingIndex].Size);
        }
        else
        {
            throw new System.Exception($"No object found for ID {id}");
        }
    }

    public PlacementState(GameObject prefab, Vector2Int size,
        Grid grid,
        PreviewSystem previewSystem,
        GridData buildingData,
        ObjectPlacer objectPlacer,
        AudioSource placementSound,
        Vector2Int gridSize)
    {
        useDirectMode = true;
        directPrefab = prefab;
        directSize = size;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.buildingData = buildingData;
        this.objectPlacer = objectPlacer;
        this.placementSound = placementSound;
        this.gridSize = gridSize;

        previewSystem.StartShowingPlacementPreview(prefab, size);
    }

    public void EndState()
    {
        previewSystem.StopShowingPlacementPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        Vector2Int buildingSize = useDirectMode ? directSize : database.buildingsData[selectedBuildingIndex].Size;
        GameObject prefab = useDirectMode ? directPrefab : database.buildingsData[selectedBuildingIndex].Prefab;
        
        bool validPlacement = CheckPlacementValidity(gridPosition) && 
                              IsBuildingWithinGrid(gridPosition, buildingSize);

        if (!validPlacement) return;
        
        placementSound.Play();
        int index = objectPlacer.PlaceObject(prefab, 
            grid.CellToWorld(gridPosition));
        
        if (useDirectMode)
        {
            buildingData.AddBuilding(gridPosition, buildingSize, 1, index);
        }
        else
        {
            GridData selectedData = database.buildingsData[selectedBuildingIndex].ID == 0 ?
                floorData :
                buildingData;
            selectedData.AddBuilding(gridPosition, 
                database.buildingsData[selectedBuildingIndex].Size, 
                database.buildingsData[selectedBuildingIndex].ID,
                index);
        }

        OnPlaced?.Invoke(objectPlacer.GetPlacedObject(index));
    }
    
    private bool CheckPlacementValidity(Vector3Int gridPosition)
    {
        if (useDirectMode)
            return buildingData.CanPlaceBuilding(gridPosition, directSize);

        GridData selectedData = database.buildingsData[selectedBuildingIndex].ID == 0 ?
            floorData :
            buildingData;
        
        return selectedData.CanPlaceBuilding(gridPosition, database.buildingsData[selectedBuildingIndex].Size);
    }
    
    private bool IsBuildingWithinGrid(Vector3Int gridPosition, Vector2Int buildingSize)
    {
        return gridPosition.x >= -gridSize.x / 2 &&
               gridPosition.x + buildingSize.x <= gridSize.x / 2 &&
               gridPosition.z >= -gridSize.y / 2 &&
               gridPosition.z + buildingSize.y <= gridSize.y / 2;
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        Vector2Int buildingSize = useDirectMode ? directSize : database.buildingsData[selectedBuildingIndex].Size;
        bool validPlacement = CheckPlacementValidity(gridPosition) &&
                              IsBuildingWithinGrid(gridPosition, buildingSize);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), validPlacement);
    }
}