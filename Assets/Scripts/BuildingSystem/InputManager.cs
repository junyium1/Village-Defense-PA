using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask placementLayerMask;
    
    private Vector3 lastMousePosition;
    RaycastHit hit;

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
