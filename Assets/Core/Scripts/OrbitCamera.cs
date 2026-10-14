using UnityEngine;
using UnityEngine.InputSystem;

// Right-drag = orbit, scroll = zoom. Put this on the Main Camera.
[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 150f;
    public float maxDistance = 50000f;
    public float rotateSpeed = 0.2f;

    float yaw, pitch = 25f;

    void Start()
    {
        var cam = GetComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100000f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    float Radius()
    {
        return target != null ? target.localScale.x * 0.5f : 1f;
    }

    // Jump to a body and frame it nicely.
    public void Focus(Transform t)
    {
        target = t;
        distance = Radius() * 5f;
    }

    void LateUpdate()
    {
        var m = Mouse.current;
        if (m == null || target == null) return;

        if (m.rightButton.isPressed)
        {
            Vector2 d = m.delta.ReadValue();
            yaw += d.x * rotateSpeed;
            pitch = Mathf.Clamp(pitch - d.y * rotateSpeed, -85f, 85f);
        }

        float scroll = m.scroll.ReadValue().y;   // about +/-120 per wheel notch on Windows
        distance = Mathf.Clamp(distance * (1f - scroll * 0.001f), Radius() * 1.3f, maxDistance);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position - transform.forward * distance;
    }
}
