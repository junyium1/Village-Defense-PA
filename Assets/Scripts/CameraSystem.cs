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
    [SerializeField] private float topDownViewHeight = 90f;
    [SerializeField] private bool enableEdgeScrolling = false;

    private bool enableMouseRotation = false;
    private bool isTopDownView = false;
    
    private Vector3 savedPosition;
    private Vector3 savedRotation;
    private Vector2 lastMousePositionRotation;
    
    private float targetFieldOfView = 50;
    [SerializeField] private float targetFieldOfViewMax = 60;
    [SerializeField] private float targetFieldOfViewMin = 10;
    
    [Tooltip("BoxCollider (trigger) définissant la zone de déplacement de la caméra. Laisser null pour désactiver le clamping.")]
    [SerializeField] private BoxCollider cameraBounds;

    // Pendant le choix d'emplacement de la zone jouable, la caméra doit pouvoir
    // survoler toute la map : le clamping est suspendu, pas supprimé.
    private bool clampEnabled = true;

    /// <summary>Change la boîte de déplacement autorisée (null = libre).</summary>
    public void SetBounds(BoxCollider bounds) => cameraBounds = bounds;

    /// <summary>Suspend / rétablit le clamping sans perdre la référence à la boîte.</summary>
    public void SetClampEnabled(bool enabled) => clampEnabled = enabled;


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
        // Pause / game over (timeScale = 0) : caméra totalement figée.
        // La rotation molette et le zoom lisent l'input souris BRUT (sans
        // Time.deltaTime), donc sans ce garde ils continueraient pendant la pause.
        if (Time.timeScale == 0f)
        {
            enableMouseRotation = false; // évite un saut de rotation à la reprise
            return;
        }

        HandleTopDownView();
        HandleCameraMovement();
        HandleCameraRotation();
        HandleCameraZoom();
        ClampCameraPosition();
    }

    private void HandleTopDownView()
    {
        if (cameraInput.Camera.ToggleTopDown.WasPressedThisFrame())
        {
            isTopDownView = !isTopDownView;

            if (isTopDownView)
            {
                savedPosition = transform.position;
                savedRotation = transform.eulerAngles;
            }
            else
            {
                transform.position = savedPosition;
                transform.eulerAngles = savedRotation;
            }
        }
    }

    private void HandleCameraMovement()
    {
        if (isTopDownView)
        {
            transform.eulerAngles = new Vector3(90f, 0f, 0f);
            Vector3 position = transform.position;
            position.y = topDownViewHeight;
            transform.position = position;
        }
        
        Vector2 inputValue = cameraInput.Camera.Movement.ReadValue<Vector2>();
        Vector3 inputDirection = new  Vector3(inputValue.x, 0, inputValue.y);
        
        Vector3 moveDirection = isTopDownView 
            ? transform.up * inputDirection.z + transform.right * inputDirection.x
            : transform.forward * inputDirection.z + transform.right * inputDirection.x;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        
        if (enableEdgeScrolling)
        {
            Vector3 edgeDirection = ApplyEdgeScrolling(Vector3.zero);
            
            Vector3 forward = isTopDownView 
                ? new Vector3(transform.up.x, 0, transform.up.z).normalized
                : new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            transform.position += (forward * edgeDirection.z + right * edgeDirection.x) * moveSpeed * Time.deltaTime;
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
        if (isTopDownView) return;
        
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

    /// <summary>
    /// Clample la position de caméra dans le BoxCollider <see cref="cameraBounds"/>.
    /// Fonctionne quelle que soit la rotation/scale du collider.
    /// </summary>
    private void ClampCameraPosition()
    {
        if (cameraBounds == null || !clampEnabled) return;

        // Position world → espace local du collider
        Vector3 localPos = cameraBounds.transform.InverseTransformPoint(transform.position);
        Vector3 halfSize = cameraBounds.size * 0.5f;
        Vector3 center = cameraBounds.center;

        // Clamp sur X et Z (Y libre pour la hauteur / zoom)
        localPos.x = Mathf.Clamp(localPos.x, center.x - halfSize.x, center.x + halfSize.x);
        localPos.z = Mathf.Clamp(localPos.z, center.z - halfSize.z, center.z + halfSize.z);

        // Retour world
        transform.position = cameraBounds.transform.TransformPoint(localPos);
    }
}
