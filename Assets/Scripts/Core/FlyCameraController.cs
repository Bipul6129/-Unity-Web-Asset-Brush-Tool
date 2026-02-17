using UnityEngine;

public class FlyCameraController : MonoBehaviour
{
    public Transform cameraPivot;

    public float lookSensitivity = 2f;
    public float baseSpeed = 8f;
    public float shiftMultiplier = 3f;
    
    // --- NEW: How close to the dirt the camera is allowed to get ---
    public float groundClearance = 1f; 

    float yaw;
    float pitch;
    float currentSpeed;

    void Start()
    {
        currentSpeed = baseSpeed;

        yaw = transform.eulerAngles.y;
        pitch = cameraPivot.localEulerAngles.x;
    }

    void Update()
    {
        bool looking = Input.GetMouseButton(1);

        if (looking)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(0, yaw, 0);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!looking) return;

        float speed = currentSpeed * (Input.GetKey(KeyCode.LeftShift) ? shiftMultiplier : 1f);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float up = 0;
        if (Input.GetKey(KeyCode.E)) up++;
        if (Input.GetKey(KeyCode.Q)) up--;

        Vector3 move =
            transform.right * h +
            transform.forward * v +
            Vector3.up * up;

        // 1. Calculate where the camera WANTS to go based on your keyboard inputs
        Vector3 targetPosition = transform.position + move.normalized * speed * Time.deltaTime;

        // --- 🛡️ NEW: THE TERRAIN SHIELD 🛡️ ---
        
        float minimumHeight = 0f; // Rule 1: Never go below absolute Y = 0

        if (Terrain.activeTerrain != null)
        {
            // Rule 2: Find out exactly how high the mountain is directly below the camera
            float terrainHeight = Terrain.activeTerrain.SampleHeight(targetPosition) + Terrain.activeTerrain.transform.position.y;
            
            // Pick whichever limit is higher: 0, or the mountain peak + 1 meter of breathing room
            minimumHeight = Mathf.Max(minimumHeight, terrainHeight + groundClearance);
        }

        // If the camera tries to sink below the shield, push it back up!
        if (targetPosition.y < minimumHeight)
        {
            targetPosition.y = minimumHeight;
        }
        // -------------------------------------

        // 2. Finally, move the camera to the safe, verified position!
        transform.position = targetPosition;
    }
}