
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform eyeTransform;
    public Transform playerTransform;// Drag your 'Player' object here
    public Vector3 offset = new Vector3(0f, 2.5f, -5f); // Default offset from player
    public float mouseSensitivity = 100f;
    public float smoothSpeed = 0.125f; // For smooth camera movement
    public float rotationSmoothSpeed = 10f; // For smooth camera rotation

    [Header("Pitch Limits")]
    public float minPitch = -30f; // Minimum vertical angle (looking up)
    public float maxPitch = 60f;  // Maximum vertical angle (looking down)

    private float currentYaw = 0f;
    private float currentPitch = 0f;

    void Awake()
    {
        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (eyeTransform == null)
        {
            Debug.LogWarning("Player Transform not assigned to TPPCameraController!");
            return;
        }

        // --- Get Mouse Input ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // --- Update Yaw and Pitch ---
        currentYaw += mouseX;
        currentPitch -= mouseY; // Subtract for inverted Y-axis mouse movement

        // Clamp the pitch to prevent flipping
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // --- Calculate Camera Rotation ---
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Smoothly rotate the camera
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed *Time.deltaTime);
        playerTransform.rotation = Quaternion.Euler(playerTransform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, playerTransform.rotation.eulerAngles.z);

        // --- Calculate Camera Position ---
        // Apply the offset rotated by the camera's current orientation
        //Vector3 desiredPosition = eyeTransform.position;
        transform.position = eyeTransform.position;
        
        // Smoothly move the camera to the desired position
        //transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}