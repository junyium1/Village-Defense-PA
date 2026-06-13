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

    public void EndState()
    {
        previewSystem.StopShowingPlacementPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        Vector2Int buildingSize = database.buildingsData[selectedBuildingIndex].Size;
        
        bool validPlacement = CheckPlacementValidity(gridPosition, selectedBuildingIndex) && 
                              IsBuildingWithinGrid(gridPosition, buildingSize);

        if (!validPlacement) return;
        
        placementSound.Play();
        int index = objectPlacer.PlaceObject(database.buildingsData[selectedBuildingIndex].Prefab, 
            grid.CellToWorld(gridPosition));
        
        GridData selectedData = database.buildingsData[selectedBuildingIndex].ID == 0 ?
            floorData :
            buildingData;
        selectedData.AddBuilding(gridPosition, 
            database.buildingsData[selectedBuildingIndex].Size, 
            database.buildingsData[selectedBuildingIndex].ID,
            index);
        
        
    }
    
    private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedBuildingIndex)
    {
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
        Vector2Int buildingSize = database.buildingsData[selectedBuildingIndex].Size;
        bool validPlacement = CheckPlacementValidity(gridPosition, selectedBuildingIndex) &&
                              IsBuildingWithinGrid(gridPosition, buildingSize);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), validPlacement);
    }
}