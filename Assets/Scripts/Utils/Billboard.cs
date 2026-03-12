using UnityEngine;

/// <summary>
/// Makes a sprite always face the camera (billboard effect)
/// Attach to any GameObject with a sprite/texture to make it face the camera
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Make the sprite face the camera
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
