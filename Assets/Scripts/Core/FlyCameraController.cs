using UnityEngine;

public class FlyCameraController : MonoBehaviour
{
    public Transform cameraPivot;

    public float lookSensitivity = 2f;
    public float baseSpeed = 8f;
    public float shiftMultiplier = 3f;
    
    // Minimum vertical distance maintained above the terrain surface
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

        // Calculate intended movement position before terrain verification
        Vector3 targetPosition = transform.position + move.normalized * speed * Time.deltaTime;

        // Enforce height constraints to prevent the camera from clipping into the terrain
        float minimumHeight = 0f; 

        if (Terrain.activeTerrain != null)
        {
            float terrainHeight = Terrain.activeTerrain.SampleHeight(targetPosition) + Terrain.activeTerrain.transform.position.y;
            minimumHeight = Mathf.Max(minimumHeight, terrainHeight + groundClearance);
        }

        // Clamp the Y position if the camera attempts to move below the terrain boundary
        if (targetPosition.y < minimumHeight)
        {
            targetPosition.y = minimumHeight;
        }

        transform.position = targetPosition;
    }
}