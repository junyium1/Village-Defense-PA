using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask placementLayerMask;

    private Vector3 lastMousePosition;
    RaycastHit hit;
    
    public event Action OnClicked, OnExit;

    private void Update()
    {
        // Garde pause : timeScale = 0 (menu pause) -> aucun placement au clic,
        // le clic appartient au menu (avant : seul le raycast UI 2D le bloquait).
        if (Input.GetMouseButtonDown(0) && Time.timeScale > 0f)
        {
            OnClicked?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnExit?.Invoke();
        }
    }

    public bool IsPointerOverUIObject()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public Vector3 GetSelectedMapPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = mainCamera.nearClipPlane;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out hit, 500, placementLayerMask))
        {
            lastMousePosition = hit.point;
        }
        return lastMousePosition;
    }
}
