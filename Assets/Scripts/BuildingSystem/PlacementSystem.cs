using UnityEngine;
using System.Collections.Generic;
using Game;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;

    [SerializeField] private BuildingsDatabaseSO database;

    [SerializeField] private Vector2Int gridSize = new Vector2Int(40, 40);
    [SerializeField] private GameObject gridVisualization;

    [Tooltip("Zone jouable posee par le joueur. Si assignee, la grille et la taille en viennent " +
             "et aucun batiment ne peut etre pose avant que la zone soit validee.")]
    [SerializeField] private LevelZone levelZone;

    private GridData floorData, buildingsData;

    [SerializeField] private PreviewSystem preview;

    [SerializeField] private AudioSource placementSound;
    [SerializeField] private AudioSource removingSound;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;
    
    [SerializeField] private ObjectPlacer objectPlacer;
    
    IBuildingState buildingState;
    
    private void Start()
    {
        // La zone est la source de verite : elle porte le Grid et sa taille.
        if (levelZone == null) levelZone = LevelZone.Instance;
        if (levelZone != null)
        {
            if (levelZone.Grid != null) grid = levelZone.Grid;
            gridSize = levelZone.SizeInCells;
        }

        StopPlacement();
        floorData = new();
        buildingsData = new();
    }

    /// <summary>Vrai tant que le joueur n'a pas encore valide l'emplacement de la zone.</summary>
    private bool ZoneNotReady => levelZone != null && !levelZone.IsPlaced;

    public void StartPlacement(int ID)
    {
        if (ZoneNotReady)
        {
            Debug.LogWarning("[PlacementSystem] Pose la zone de niveau avant de construire.");
            return;
        }

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
        if (ZoneNotReady) return;

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
        // Pendant le choix d'emplacement, la grille doit rester visible : c'est
        // elle que le joueur promene sur la map.
        gridVisualization.SetActive(ZoneNotReady);
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
