using UnityEngine;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private GameObject cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    
    [SerializeField] private BuildingsDatabaseSO database;
    private int selectedBuildingIndex = -1;
    
    [SerializeField] private Vector2Int gridSize = new Vector2Int(20, 20);
    [SerializeField] private GameObject gridVisualization;
    private GridData floorData, buildingsData;
    
    private Renderer[] previewRenderer;
    private List<GameObject> placedGameObjects = new();

    [SerializeField] private AudioSource placementSound;
    
    private void Start()
    {
        StopPlacement();
        floorData = new();
        buildingsData = new();
        previewRenderer = cellIndicator.GetComponentsInChildren<Renderer>();
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
        cellIndicator.SetActive(true);
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
        cellIndicator.SetActive(false);
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;
    }

    private void Update()
    {
        if (selectedBuildingIndex < 0)
        {
            return;
        }
        
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        Vector2Int buildingSize = database.buildingsData[selectedBuildingIndex].Size;

        bool validPlacement = CheckPlacementValidity(gridPosition, selectedBuildingIndex) && 
                              IsBuildingWithinGrid(gridPosition, buildingSize);
        

        if (validPlacement == false)
        {
            foreach (Renderer r in previewRenderer)
            {
                r.material.color = Color.red;
            }
        }
        else
        {
            foreach (Renderer r in previewRenderer)
            {
                r.material.color = Color.white;
            }
        }
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
    }
}
