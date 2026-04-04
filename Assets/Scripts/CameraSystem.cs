using UnityEngine;
using Unity.Cinemachine;

public class CameraSystem : MonoBehaviour
{
    private CameraInput cameraInput;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float moveSpeed = 90f;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float mouseRotateSpeed = 0.2f;
    [SerializeField] private float zoomSpeed = 15f;

    private bool enableEdgeScrolling = true;
    private bool enableMouseRotation = false;
    private Vector2 lastMousePositionRotation;
    
    private float targetFieldOfView = 50;
    [SerializeField] float targetFieldOfViewMax = 60;
    [SerializeField] float targetFieldOfViewMin = 10;
    
    
    private void Awake()
    {
        cameraInput = new CameraInput();
    }

    private void OnEnable()
    {
        cameraInput.Enable();
    }

    private void OnDisable()
    {
        cameraInput.Disable();
    }
    
    private void Update()
    {
        HandleCameraMovement();
        HandleCameraRotation();
        HandleCameraZoom();
    }

    private void HandleCameraMovement()
    {
        Vector2 inputValue = cameraInput.Camera.Movement.ReadValue<Vector2>();
        Vector3 inputDirection = new  Vector3(inputValue.x, 0, inputValue.y);
        
        Vector3 moveDirection = transform.forward * inputDirection.z + transform.right * inputDirection.x;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        
        if (enableEdgeScrolling)
        {
            Vector3 edgeDirection = ApplyEdgeScrolling(Vector3.zero);
            
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            transform.position += (forward * edgeDirection.z + right * edgeDirection.x) * moveSpeed * Time.deltaTime;inputDirection = ApplyEdgeScrolling(inputDirection);
        }
    }

    private Vector3 ApplyEdgeScrolling(Vector3 inputDirection)
    {
        int edgeScrollSize = 20;
        if (Input.mousePosition.x < edgeScrollSize)
        {
            inputDirection.x = -1f;
        }
        if (Input.mousePosition.y < edgeScrollSize)
        {
            inputDirection.z = -1f;
        }
        if (Input.mousePosition.x > Screen.width - edgeScrollSize)
        {
            inputDirection.x = +1f;
        }
        if (Input.mousePosition.y > Screen.height - edgeScrollSize)
        {
            inputDirection.z = +1f;
        }
        
        return inputDirection;
    }

    private void HandleCameraRotation()
    {
        float rotateDirection = cameraInput.Camera.Rotation.ReadValue<float>();
        transform.eulerAngles += new Vector3(0, rotateDirection * rotateSpeed * Time.deltaTime, 0);

        ApplyCameraMouseRotation();
    }

    private void ApplyCameraMouseRotation()
    {
        if (Input.GetMouseButtonDown(2))
        {
            enableMouseRotation = true;
            lastMousePositionRotation = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            enableMouseRotation = false;
        }

        if (enableMouseRotation)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePositionRotation;
            
            transform.Rotate(Vector3.up, delta.x * mouseRotateSpeed, Space.World);
            transform.Rotate(Vector3.right, -delta.y * mouseRotateSpeed, Space.Self);
            
            lastMousePositionRotation = Input.mousePosition;
        }
    }

    private void HandleCameraZoom()
    {
        if (Input.mouseScrollDelta.y > 0)
        {
            targetFieldOfView -= 5;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            targetFieldOfView += 5;
        }
        
        targetFieldOfView = Mathf.Clamp(targetFieldOfView, targetFieldOfViewMin, targetFieldOfViewMax);
        
        cinemachineCamera.Lens.FieldOfView = 
            Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFieldOfView, zoomSpeed * Time.deltaTime);
    }
}
