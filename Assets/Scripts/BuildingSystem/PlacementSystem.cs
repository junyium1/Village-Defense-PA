using UnityEngine;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    
    [SerializeField] private BuildingsDatabaseSO database;
    
    [SerializeField] private Vector2Int gridSize = new Vector2Int(40, 40);
    [SerializeField] private GameObject gridVisualization;
    private GridData floorData, buildingsData;
    
    [SerializeField] private PreviewSystem preview;

    [SerializeField] private AudioSource placementSound;
    [SerializeField] private AudioSource removingSound;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;
    
    [SerializeField] private ObjectPlacer objectPlacer;
    
    IBuildingState buildingState;
    
    private void Start()
    {
        StopPlacement();
        floorData = new();
        buildingsData = new();
    }

    public void StartPlacement(int ID)
    {
        StopPlacement();
        gridVisualization.SetActive(true);
        buildingState = new PlacementState(ID, 
            grid, 
            preview, 
            database, 
            floorData, 
            buildingsData, 
            objectPlacer, 
            placementSound, 
            gridSize);
        
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    public void StartRemoving()
    {
        StopPlacement();
        gridVisualization.SetActive(true);
        buildingState = new RemovingState(grid, preview, floorData, buildingsData, objectPlacer, removingSound, gridSize);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }
    
    private void PlaceStructure()
    {
        if (inputManager.IsPointerOverUIObject())
        {
            return;
        }
        
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        
        buildingState.OnAction(gridPosition);
    }

    
    private void StopPlacement()
    {
        gridVisualization.SetActive(false);
        buildingState?.EndState();
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;
        lastDetectedPosition = Vector3Int.zero;
        buildingState = null;
    }

    private void Update()
    {
        if (buildingState == null)
            return;
    
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        if (lastDetectedPosition != gridPosition)
        {
            buildingState.UpdateState(gridPosition);
            lastDetectedPosition = gridPosition;
        }
    }
}
