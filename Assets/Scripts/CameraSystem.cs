using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    private CameraInput cameraInput;
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float dragPanSpeed = 2f;
    [SerializeField] private float rotateSpeed = 50f;
    [SerializeField] private float zoomSpeed = 50f;

    private bool enableEdgeScrolling = true;
    private bool enableDragPanMovement = false;
    private Vector2 lastMousePosition;
    
    
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
    }

    private void HandleCameraMovement()
    {
        Vector2 inputValue = cameraInput.Camera.Movement.ReadValue<Vector2>();
        Vector3 inputDirection = new  Vector3(inputValue.x, 0, inputValue.y);

        if (enableEdgeScrolling)
        {
            inputDirection = ApplyEdgeScrolling(inputDirection);
        }
        
        //inputDirection = ApplyDragPanMovement(inputDirection);
        
        Vector3 moveDirection = transform.forward * inputDirection.z + transform.right * inputDirection.x;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
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

    private Vector3 ApplyDragPanMovement(Vector3 inputDirection)
    {
        if (Input.GetMouseButtonDown(2))
        {
            enableDragPanMovement = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            enableDragPanMovement = false;
        }

        if (enableDragPanMovement)
        {
            Vector2 mouseMovementDelta = (Vector2)Input.mousePosition - lastMousePosition;
            
            inputDirection.x = mouseMovementDelta.x;
            inputDirection.z = mouseMovementDelta.y * dragPanSpeed;
            
            lastMousePosition = Input.mousePosition;
        }

        return inputDirection;
    }

    private void HandleCameraRotation()
    {
        float rotateDirection = cameraInput.Camera.Rotation.ReadValue<float>();
        transform.eulerAngles += new Vector3(0, rotateDirection * rotateSpeed * Time.deltaTime, 0);
    }
}
