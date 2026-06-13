using UnityEngine;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    
    [SerializeField] private BuildingsDatabaseSO database;
    private int selectedBuildingIndex = -1;
    
    [SerializeField] private Vector2Int gridSize = new Vector2Int(20, 20);
    [SerializeField] private GameObject gridVisualization;
    private GridData floorData, buildingsData;
    
    private List<GameObject> placedGameObjects = new();
    
    [SerializeField] private PreviewSystem preview;

    [SerializeField] private AudioSource placementSound;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;
    
    private void Start()
    {
        StopPlacement();
        floorData = new();
        buildingsData = new();
    }

    public void StartPlacement(int ID)
    {
        StopPlacement();
        selectedBuildingIndex = database.buildingsData.FindIndex(data => data.ID == ID);
        if (selectedBuildingIndex < 0)
        {
            Debug.LogError($"No ID found in database {ID}");
            return;
        }
        
        gridVisualization.SetActive(true);
        preview.StartShowingPlacementPreview(
            database.buildingsData[selectedBuildingIndex].Prefab, 
            database.buildingsData[selectedBuildingIndex].Size);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    private bool IsBuildingWithinGrid(Vector3Int gridPosition, Vector2Int buildingSize)
    {
        if (gridPosition.x >= -gridSize.x / 2 &&
            gridPosition.x + buildingSize.x <= gridSize.x / 2 &&
            gridPosition.z >= -gridSize.y / 2 &&
            gridPosition.z + buildingSize.y <= gridSize.y / 2)
        {
            return true;
        }

        return false;
    }

    private void PlaceStructure()
    {
        if (inputManager.IsPointerOverUIObject())
        {
            return;
        }
        
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        Vector2Int buildingSize = database.buildingsData[selectedBuildingIndex].Size;
        
        bool validPlacement = CheckPlacementValidity(gridPosition, selectedBuildingIndex) && 
                              IsBuildingWithinGrid(gridPosition, buildingSize);

        if (validPlacement)
        {
            placementSound.Play();
            GameObject newBuilding = Instantiate(database.buildingsData[selectedBuildingIndex].Prefab);
            newBuilding.transform.position = grid.CellToWorld(gridPosition);
            placedGameObjects.Add(newBuilding);
            GridData selectedData = database.buildingsData[selectedBuildingIndex].ID == 0 ?
                floorData :
                buildingsData;
            selectedData.AddBuilding(gridPosition, 
                database.buildingsData[selectedBuildingIndex].Size, 
                database.buildingsData[selectedBuildingIndex].ID,
                placedGameObjects.Count -1);
        }
        else
        {
            return;
        }
        
        preview.UpdatePosition(grid.CellToWorld(gridPosition), false);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedBuildingIndex)
    {
        GridData selectedData = database.buildingsData[selectedBuildingIndex].ID == 0 ?
            floorData :
            buildingsData;
        
        return selectedData.CanPlaceBuilding(gridPosition, database.buildingsData[selectedBuildingIndex].Size);
    }
    
    private void StopPlacement()
    {
        selectedBuildingIndex = -1;
        gridVisualization.SetActive(false);
        preview.StopShowingPlacementPreview();
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;
        lastDetectedPosition = Vector3Int.zero;
    }

    private void Update()
    {
        if (selectedBuildingIndex < 0)
            return;
    
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        Vector2Int buildingSize = database.buildingsData[selectedBuildingIndex].Size;

        if (lastDetectedPosition != gridPosition)
        {
            bool validPlacement = CheckPlacementValidity(gridPosition, selectedBuildingIndex) && 
                                  IsBuildingWithinGrid(gridPosition, buildingSize);
    
            preview.UpdatePosition(grid.CellToWorld(gridPosition), validPlacement);
        }
    }
}
