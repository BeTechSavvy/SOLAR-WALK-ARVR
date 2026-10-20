using System.Collections.Generic;
using UnityEngine;

// Draws each planet's orbit as a thin circle (LineRenderer in the SolarSystem's local space,
// so it moves and scales with the system in AR). Put this on the SolarSystem object.
public class OrbitLines : MonoBehaviour
{
    public SolarSystem solar;
    public Material lineMaterial;      // URP Unlit material (Set Up Project creates one)
    public Color color = new Color(0.45f, 0.55f, 0.85f, 1f);
    public int segments = 128;
    public float widthFactor = 0.004f; // line width relative to the orbit radius

    class Entry { public PlanetInfo body; public LineRenderer line; public float radius; }
    readonly List<Entry> orbits = new List<Entry>();

    void Awake()
    {
        if (solar == null) solar = GetComponent<SolarSystem>();
    }

    void Build()
    {
        if (lineMaterial == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            lineMaterial = new Material(sh);
        }
        var mat = new Material(lineMaterial);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        foreach (var p in solar.Bodies)
        {
            if (p.periodDays <= 0f) continue;   // the Sun has no orbit
            var go = new GameObject(p.planetName + " Orbit");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.sharedMaterial = mat;
            lr.startColor = lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            orbits.Add(new Entry { body = p, line = lr, radius = -1f });
        }
    }

    void LateUpdate()
    {
        if (solar == null) return;
        if (orbits.Count == 0)
        {
            if (!solar.Ready) return;
            Build();
        }

        float scale = transform.lossyScale.x;
        foreach (var e in orbits)
        {
            Vector3 lp = e.body.transform.localPosition;
            float r = new Vector2(lp.x, lp.z).magnitude;
            if (r <= 0f) continue;

            if (Mathf.Abs(r - e.radius) > r * 0.001f)   // changes when Real/Visible scale is toggled
            {
                e.radius = r;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments;
                    e.line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
            }
            e.line.widthMultiplier = Mathf.Max(r * widthFactor, 0.05f) * scale;
        }
    }
}
