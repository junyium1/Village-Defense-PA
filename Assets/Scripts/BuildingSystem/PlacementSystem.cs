using System;
using Unity.Multiplayer.Center.Common;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private GameObject mouseIndicator;
    [SerializeField] private GameObject cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    
    [SerializeField] private BuildingsDatabaseSO database;
    private int selectedBuildingIndex = -1;
    
    [SerializeField] private GameObject gridVisualization;

    private void Start()
    {
        StopPlacement();
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

    private void PlaceStructure()
    {
        if (inputManager.IsPointerOverUIObject())
        {
            return;
        }
        
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        GameObject newBuilding = Instantiate(database.buildingsData[selectedBuildingIndex].Prefab);
        
        newBuilding.transform.position = grid.CellToWorld(gridPosition);
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
        mouseIndicator.transform.position = mousePosition;
        
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
    }
}
