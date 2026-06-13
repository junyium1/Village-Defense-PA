using UnityEngine;

public class RemovingState : IBuildingState
{
    private int gameObjectIndex = -1;
    Grid grid;
    PreviewSystem previewSystem;
    GridData floorData;
    GridData buildingData;
    ObjectPlacer objectPlacer;
    AudioSource removingSound;
    Vector2Int gridSize;

    public RemovingState(Grid grid,
        PreviewSystem previewSystem,
        GridData floorData,
        GridData buildingData,
        ObjectPlacer objectPlacer,
        AudioSource placementSound,
        Vector2Int gridSize)
    {
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.floorData = floorData;
        this.buildingData = buildingData;
        this.objectPlacer = objectPlacer;
        this.removingSound = placementSound;
        this.gridSize = gridSize;
        
        previewSystem.StartShowingRemovePreview();
    }

    public void EndState()
    {
        previewSystem.StopShowingPlacementPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        GridData selectedData = null;
        if (buildingData.CanPlaceBuilding(gridPosition, Vector2Int.one) == false)
        {
            selectedData = buildingData;
        }
        else if(floorData.CanPlaceBuilding(gridPosition, Vector2Int.one) == false)
        {
            selectedData = floorData;
        }

        if (selectedData == null)
        {
            return;
        }

        gameObjectIndex = selectedData.GetRepresentationIndex(gridPosition);
    
        if (gameObjectIndex == -1) return;
    
        selectedData.RemoveObjectAt(gridPosition);
        objectPlacer.RemoveObjectAt(gameObjectIndex);
        removingSound.Play();
    }

    private bool CheckIfSelectioncIsValid(Vector3Int gridPosition)
    {
        return !(buildingData.CanPlaceBuilding(gridPosition, Vector2Int.one) &&
                floorData.CanPlaceBuilding(gridPosition, Vector2Int.one));
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool validity = CheckIfSelectioncIsValid(gridPosition);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), validity);
    }
}
