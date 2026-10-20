using UnityEngine;
using UnityEngine.InputSystem;

// First-person "fly through space" camera, used by the Walk and Marker scenes when AR
// is not available (Unity Editor / PC preview).
//   WASD / arrows = move, Q / E = down / up, Shift = faster, right-drag = look, scroll = zoom.
//   Click a planet (HUD) -> the camera turns to it, flies there and follows it along its orbit.
public class FlyCamera : MonoBehaviour
{
    public SolarSystem solar;
    public float moveSpeed = 2f;      // metres (scene units) per second
    public float lookSpeed = 0.15f;
    public bool flyToSelection = true;

    float yaw, pitch;
    Transform follow;
    float followDist;
    bool subscribed;
    bool skipFirst;   // ignore the automatic "select the Sun" at start-up

    void OnEnable()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
        skipFirst = solar == null || !solar.Ready;
        Subscribe();
    }

    void OnDisable()
    {
        if (solar != null && subscribed) solar.Selected -= OnSelected;
        subscribed = false;
    }

    void Subscribe()
    {
        if (solar == null || subscribed) return;
        solar.Selected += OnSelected;
        subscribed = true;
    }

    void OnSelected(PlanetInfo p)
    {
        if (skipFirst) { skipFirst = false; return; }
        if (!flyToSelection || p == null) return;
        follow = p.transform;
        followDist = Mathf.Max(p.transform.lossyScale.x * 0.5f * 6f, 0.02f);

        // Turn towards the planet.
        Vector3 dir = follow.position - transform.position;
        if (dir.sqrMagnitude > 1e-8f)
        {
            dir.Normalize();
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }

    void Update()
    {
        Subscribe();   // in case solar was assigned after OnEnable

        var kb = Keyboard.current;
        var m = Mouse.current;

        // Look
        if (m != null && m.rightButton.isPressed)
        {
            Vector2 d = m.delta.ReadValue();
            yaw += d.x * lookSpeed;
            pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -89f, 89f);
        }
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            Vector2 d = ts.primaryTouch.delta.ReadValue();
            yaw += d.x * lookSpeed;
            pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -89f, 89f);
        }
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Move
        Vector3 move = Vector3.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move += Vector3.forward;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move += Vector3.back;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move += Vector3.left;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += Vector3.right;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move += Vector3.down;
        }
        float speed = moveSpeed * (kb != null && kb.leftShiftKey.isPressed ? 4f : 1f);

        if (move != Vector3.zero)
        {
            follow = null;   // manual control cancels follow
            transform.position += transform.rotation * move.normalized * speed * Time.deltaTime;
        }
        else if (follow != null)
        {
            if (m != null)
            {
                float scroll = m.scroll.ReadValue().y;
                followDist *= 1f - Mathf.Clamp(scroll * 0.001f, -0.5f, 0.5f);
            }
            Vector3 goal = follow.position - transform.forward * followDist;
            transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-3f * Time.deltaTime));
        }
        else if (m != null)
        {
            float scroll = m.scroll.ReadValue().y;
            transform.position += transform.forward * scroll * 0.002f * moveSpeed;
        }
    }
}
