using UnityEngine;
using UnityEngine.InputSystem;

// Right-drag = orbit, scroll = zoom (mouse).
// One-finger drag = orbit, two-finger pinch = zoom (touch, so desktop mode also works on a phone).
// Put this on the Main Camera.
[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 150f;
    public float maxDistance = 50000f;
    public float rotateSpeed = 0.2f;

    float yaw, pitch = 25f;
    float lastPinch;

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
        return target != null ? target.lossyScale.x * 0.5f : 1f;
    }

    // Jump to a body and frame it nicely.
    public void Focus(Transform t)
    {
        target = t;
        distance = Radius() * 5f;
    }

    void LateUpdate()
    {
        if (target == null) return;

        var m = Mouse.current;
        if (m != null)
        {
            if (m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue();
                yaw += d.x * rotateSpeed;
                pitch = Mathf.Clamp(pitch - d.y * rotateSpeed, -85f, 85f);
            }

            float scroll = m.scroll.ReadValue().y;   // about +/-120 per wheel notch on Windows
            distance *= 1f - Mathf.Clamp(scroll * 0.001f, -0.5f, 0.5f);
        }

        var ts = Touchscreen.current;
        if (ts != null)
        {
            int down = 0;
            Vector2 p0 = Vector2.zero, p1 = Vector2.zero, d0 = Vector2.zero;
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed) continue;
                if (down == 0) { p0 = t.position.ReadValue(); d0 = t.delta.ReadValue(); }
                else if (down == 1) p1 = t.position.ReadValue();
                down++;
            }

            if (down == 1)
            {
                yaw += d0.x * rotateSpeed;
                pitch = Mathf.Clamp(pitch - d0.y * rotateSpeed, -85f, 85f);
                lastPinch = 0f;
            }
            else if (down >= 2)
            {
                float pinch = Vector2.Distance(p0, p1);
                if (lastPinch > 0f && pinch > 0f) distance *= lastPinch / pinch;
                lastPinch = pinch;
            }
            else lastPinch = 0f;
        }

        distance = Mathf.Clamp(distance, Radius() * 1.3f, maxDistance);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position - transform.forward * distance;
    }
}
