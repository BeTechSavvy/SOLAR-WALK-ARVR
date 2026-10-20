using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// AR placement for the Marker (table) and Walk (room-scale) scenes.
//  1. Scan: AR Foundation detects horizontal planes (table / floor).
//  2. Tap:  a ray from the tap point is cast against the detected planes (ARRaycastManager)
//           and the solar system is anchored at the hit pose.
//  3. Pinch with two fingers to resize it. "Re-place" in the HUD starts again.
// Walk mode uses a much bigger scale (~13 m across) so you physically walk between planets.
// When AR is not available (Unity Editor / PC) it falls back to a preview with FlyCamera.
public class ARSolarPlacer : MonoBehaviour
{
    [Header("Assign in Inspector (Set Up Project does this)")]
    public bool walkMode;
    public SolarSystem solar;            // on the "Solar Root" object, starts inactive
    public SolarHUD hud;
    public Camera cam;
    public ARRaycastManager raycaster;
    public ARPlaneManager planes;
    public FlyCamera fly;                // used only in the no-AR preview

    [Header("Scale (metres per scene unit)")]
    public float startScale = 0.003f;    // table: Neptune's orbit ~1.3 m across. Walk uses 0.03 (~13 m)

    static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

    bool arReady, placed, fallback;
    float scanStart, lastPinch;

    IEnumerator Start()
    {
        if (cam == null) cam = Camera.main;
        if (hud != null) hud.TapOnWorld += OnTap;

        solar.transform.localScale = Vector3.one * startScale;
        solar.gameObject.SetActive(false);
        scanStart = Time.time;

        // AR only runs on the phone. In the Editor / on PC, show a preview instead.
        if (!Application.isMobilePlatform)
        {
            StartFallback();
            yield break;
        }

        Hint("Starting AR...");
        if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            Hint("Installing Google Play Services for AR...");
            yield return ARSession.Install();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            Hint("This phone does not support ARCore.");
            StartFallback();
            yield break;
        }

        arReady = true;
        scanStart = Time.time;
    }

    void OnDestroy()
    {
        if (hud != null) hud.TapOnWorld -= OnTap;
    }

    void Update()
    {
        if (!arReady || fallback) return;

        if (!placed)
        {
            int count = planes != null ? planes.trackables.count : 0;
            string surface = walkMode ? "the floor" : "a table";
            if (count > 0)
                Hint("Surface found! Tap on " + surface + " to place the solar system.");
            else if (Time.time - scanStart > 8f)
                Hint("Still scanning... keep moving the phone slowly, or tap anywhere to place it in front of you.");
            else
                Hint("Point the camera at " + surface + " and move the phone slowly to scan it.");
            return;
        }

        HandlePinch();
    }

    // --------------------------------------------------------------- placing

    void OnTap(Vector2 screenPos)
    {
        if (!arReady || placed || fallback) return;

        if (raycaster != null && raycaster.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
        {
            Place(Hits[0].pose.position);
        }
        else if (Time.time - scanStart > 8f)
        {
            // No plane yet: put it in front of the camera so the demo never gets stuck.
            Vector3 f = Flat(cam.transform.forward);
            Place(cam.transform.position + f * (walkMode ? 1.5f : 0.6f) + Vector3.down * 0.4f);
        }
    }

    void Place(Vector3 pos)
    {
        var root = solar.transform;
        if (walkMode) pos.y = cam.transform.position.y - 0.4f;   // planets at chest height
        else pos.y += 0.05f;                                      // hover just above the table

        root.position = pos;
        root.rotation = Quaternion.LookRotation(Flat(cam.transform.forward), Vector3.up);
        solar.gameObject.SetActive(true);
        placed = true;
        SetPlanesVisible(false);

        Hint(walkMode
            ? "Walk around the Sun! Tap a planet for info, pinch to resize, Next to cycle planets."
            : "Pinch to resize, tap a planet for info. Walk around the table to see it from all sides.");
    }

    // Called by the HUD's "Re-place" button.
    public void ResetPlacement()
    {
        if (fallback) return;
        placed = false;
        solar.gameObject.SetActive(false);
        SetPlanesVisible(true);
        scanStart = Time.time;
    }

    void SetPlanesVisible(bool visible)
    {
        if (planes == null) return;
        planes.enabled = visible;
        foreach (var p in planes.trackables) p.gameObject.SetActive(visible);
    }

    // ---------------------------------------------------------------- pinch

    void HandlePinch()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        int down = 0;
        Vector2 a = Vector2.zero, b = Vector2.zero;
        foreach (var t in ts.touches)
        {
            if (!t.press.isPressed) continue;
            if (down == 0) a = t.position.ReadValue();
            else if (down == 1) b = t.position.ReadValue();
            down++;
        }

        if (down < 2) { lastPinch = 0f; return; }

        float dist = Vector2.Distance(a, b);
        if (lastPinch > 0f && dist > 0f)
        {
            float s = solar.transform.localScale.x * dist / lastPinch;
            s = Mathf.Clamp(s, startScale * 0.2f, startScale * 10f);
            solar.transform.localScale = Vector3.one * s;
        }
        lastPinch = dist;
    }

    // -------------------------------------------------------------- fallback

    void StartFallback()
    {
        fallback = true;
        if (hud != null) hud.SetFallback(true);
        if (planes != null) planes.enabled = false;

        // Stop the AR camera components so the camera can be moved by FlyCamera.
        foreach (var bh in cam.GetComponents<Behaviour>())
        {
            string n = bh.GetType().Name;
            if (n == "TrackedPoseDriver" || n == "ARCameraManager" || n == "ARCameraBackground") bh.enabled = false;
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.01f;

        Vector3 f = Flat(cam.transform.forward);
        var root = solar.transform;
        root.position = cam.transform.position + f * (walkMode ? 2.5f : 0.9f) + Vector3.down * (walkMode ? 0.4f : 0.2f);
        root.rotation = Quaternion.identity;
        solar.gameObject.SetActive(true);
        placed = true;

        if (fly != null)
        {
            fly.solar = solar;
            fly.moveSpeed = startScale * 60f;
            fly.enabled = true;
        }

        Hint("Preview mode (AR runs on the phone).  WASD move, Q/E down/up, Shift faster, right-drag look, click a planet to fly to it.");
    }

    // ---------------------------------------------------------------- helpers

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 1e-4f ? Vector3.forward : v.normalized;
    }

    void Hint(string text)
    {
        if (hud != null) hud.SetHint(text);
    }
}
