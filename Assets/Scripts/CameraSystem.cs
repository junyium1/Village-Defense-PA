using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSystem : MonoBehaviour
{
    private CameraInput cameraInput;
    [SerializeField] private float moveSpeed = 50f;

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
        Vector2 inputDirection = cameraInput.Camera.Movement.ReadValue<Vector2>();
        
        Vector3 moveDirection = transform.forward * inputDirection.y + transform.right * inputDirection.x;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        
    }
}
