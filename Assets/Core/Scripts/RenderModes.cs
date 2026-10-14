using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Unit 5 demo: polygon (solid) rendering vs wireframe rendering, plus a lighting toggle.
// Put this on any empty GameObject in the Desktop scene (e.g. "RenderModes").
//
// How the wireframe works: the planet sphere's triangles are converted to a mesh made of
// LINES (MeshTopology.Lines), so you see the real polygon edges. No special shader needed,
// so it works in URP and in the built .exe.
//
// Keys: W = cycle render mode, L = toggle ambient light.
public class RenderModes : MonoBehaviour
{
    public TMP_Text statsLabel;   // optional: shows mode + polygon counts

    enum Mode { Solid, Wireframe, SolidPlusWire }

    Mode mode = Mode.Solid;
    bool ambientOn;

    readonly List<PlanetInfo> bodies = new List<PlanetInfo>();
    readonly List<GameObject> overlays = new List<GameObject>();
    Mesh solidMesh, wireMesh;
    int triangles, lineCount;

    IEnumerator Start()
    {
        yield return null;   // wait one frame so SolarSystem has created the bodies

        bodies.AddRange(FindObjectsByType<PlanetInfo>(FindObjectsSortMode.None));
        if (bodies.Count == 0)
        {
            Debug.LogWarning("RenderModes: no PlanetInfo bodies found. Is SolarSystem in the scene?");
            yield break;
        }

        solidMesh = bodies[0].GetComponent<MeshFilter>().sharedMesh;
        if (!solidMesh.isReadable)
        {
            Debug.LogError("RenderModes: sphere mesh is not readable, cannot build wireframe.");
            yield break;
        }

        triangles = solidMesh.triangles.Length / 3;
        wireMesh = BuildWireMesh(solidMesh);
        lineCount = (int)wireMesh.GetIndexCount(0) / 2;

        // One white wireframe child per body, used for the "Solid + wireframe" mode.
        foreach (var p in bodies)
        {
            var go = new GameObject("WireOverlay");
            go.transform.SetParent(p.transform, false);
            go.transform.localScale = Vector3.one * 1.003f;   // lifted slightly to avoid z-fighting
            go.AddComponent<MeshFilter>().sharedMesh = wireMesh;
            var r = go.AddComponent<MeshRenderer>();
            var src = p.GetComponent<Renderer>();
            r.sharedMaterial = new Material(src.sharedMaterial) { color = Color.white };
            go.SetActive(false);
            overlays.Add(go);
        }

        Apply();
    }

    static Mesh BuildWireMesh(Mesh src)
    {
        int[] tris = src.triangles;
        var seen = new HashSet<ulong>();
        var lines = new List<int>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            AddEdge(seen, lines, tris[i], tris[i + 1]);
            AddEdge(seen, lines, tris[i + 1], tris[i + 2]);
            AddEdge(seen, lines, tris[i + 2], tris[i]);
        }

        var m = new Mesh();
        m.name = src.name + "_Wire";
        m.vertices = src.vertices;
        m.normals = src.normals;
        m.uv = src.uv;
        m.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
        return m;
    }

    static void AddEdge(HashSet<ulong> seen, List<int> lines, int a, int b)
    {
        ulong lo = (ulong)Mathf.Min(a, b);
        ulong hi = (ulong)Mathf.Max(a, b);
        if (seen.Add((hi << 32) | lo))
        {
            lines.Add(a);
            lines.Add(b);
        }
    }

    // --- Hooks for UI buttons ----------------------------------------------
    public void CycleMode()
    {
        if (bodies.Count == 0 || wireMesh == null) return;
        mode = (Mode)(((int)mode + 1) % 3);
        Apply();
    }

    public void ToggleAmbient()
    {
        ambientOn = !ambientOn;
        ApplyAmbient();
        UpdateLabel();
    }
    // -----------------------------------------------------------------------

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame) CycleMode();
        if (kb.lKey.wasPressedThisFrame) ToggleAmbient();
    }

    void Apply()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            var mf = bodies[i].GetComponent<MeshFilter>();
            mf.sharedMesh = (mode == Mode.Wireframe) ? wireMesh : solidMesh;
            overlays[i].SetActive(mode == Mode.SolidPlusWire);
        }
        ApplyAmbient();
        UpdateLabel();
    }

    void ApplyAmbient()
    {
        // Wireframe-only mode gets extra ambient so lines on the night side stay visible.
        bool bright = ambientOn || mode == Mode.Wireframe;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = bright ? new Color(0.55f, 0.55f, 0.6f) : new Color(0.06f, 0.06f, 0.09f);
    }

    void UpdateLabel()
    {
        if (statsLabel == null) return;

        string modeName = mode == Mode.Solid ? "Solid (polygon) rendering"
                        : mode == Mode.Wireframe ? "Wireframe rendering"
                        : "Solid + wireframe overlay";
        string light = ambientOn ? "Sun + ambient light" : "Sun only (local illumination)";

        statsLabel.text =
            "Render: " + modeName +
            "\nLighting: " + light +
            "\nPolygons per body: " + triangles.ToString("N0") +
            "  |  total (" + bodies.Count + " bodies): " + (triangles * bodies.Count).ToString("N0") +
            "\nWireframe edges per body: " + lineCount.ToString("N0") +
            "\n[W] render mode   [L] lighting";
    }
}
