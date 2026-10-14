using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Builds the Sun + 8 planets from real data and simulates their orbits.
// Orbital periods and rotation periods keep their REAL ratios.
// Put this on an empty GameObject at position (0,0,0).
public class SolarSystem : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public OrbitCamera orbit;        // the OrbitCamera on Main Camera
    public Light sunLight;           // the scene's Directional Light
    public Material sunMaterial;     // optional emissive material for the Sun

    [Header("Simulation")]
    public float daysPerSecond = 20f;   // simulated Earth days per real second
    public float spinSpeed = 20f;       // spin speed multiplier (relative spin rates stay real)
    public bool realScale = false;      // false = "visible" scale, true = true-to-scale

    public Transform Sun { get; private set; }
    public event System.Action<PlanetInfo> Selected;

    readonly List<PlanetInfo> bodies = new List<PlanetInfo>();
    float simDays;

    static readonly Key[] FocusKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8
    };

    struct Row
    {
        public string name;
        public float diameterKm, distanceMkm, periodDays, rotationHours;
        public Color color;
        public string info;

        public Row(string n, float dia, float dist, float period, float rot, Color c, string i)
        {
            name = n; diameterKm = dia; distanceMkm = dist;
            periodDays = period; rotationHours = rot; color = c; info = i;
        }
    }

    static Row[] Rows()
    {
        return new[]
        {
            new Row("Sun", 1392700f, 0f, 0f, 609.1f, new Color(1f, 0.85f, 0.2f),
                "Our star: a ball of hot plasma holding about 99.8% of the solar system's mass. Surface temperature is around 5,500 C."),
            new Row("Mercury", 4879f, 57.9f, 88f, 1407.6f, new Color(0.6f, 0.6f, 0.6f),
                "Smallest planet and closest to the Sun. One day (sunrise to sunrise) lasts about 176 Earth days."),
            new Row("Venus", 12104f, 108.2f, 224.7f, -5832.5f, new Color(0.9f, 0.75f, 0.45f),
                "Hottest planet (about 465 C) because of its thick CO2 atmosphere. It spins backwards (retrograde), very slowly."),
            new Row("Earth", 12756f, 149.6f, 365.2f, 23.9f, new Color(0.2f, 0.45f, 0.9f),
                "Our home. The only known world with liquid surface water and life. It has one moon."),
            new Row("Mars", 6792f, 227.9f, 687f, 24.6f, new Color(0.85f, 0.35f, 0.2f),
                "The red planet, coloured by iron oxide dust. Home to Olympus Mons, the tallest volcano in the solar system."),
            new Row("Jupiter", 142984f, 778.6f, 4331f, 9.9f, new Color(0.85f, 0.65f, 0.45f),
                "Largest planet: more than 1,300 Earths would fit inside. The Great Red Spot is a giant storm that has raged for centuries."),
            new Row("Saturn", 120536f, 1433.5f, 10747f, 10.7f, new Color(0.9f, 0.8f, 0.55f),
                "Famous for its rings of ice and rock. It is less dense than water and has well over 100 known moons."),
            new Row("Uranus", 51118f, 2872.5f, 30589f, -17.2f, new Color(0.55f, 0.85f, 0.9f),
                "An ice giant that rotates on its side, tilted about 98 degrees. One orbit takes 84 years."),
            new Row("Neptune", 49528f, 4495.1f, 59800f, 16.1f, new Color(0.2f, 0.3f, 0.85f),
                "The farthest planet: a windy ice giant with the fastest winds in the solar system. One orbit takes about 165 years."),
        };
    }

    void Start()
    {
        // Dark space: almost no ambient light, so the Sun does the lighting.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.06f, 0.09f);

        foreach (var r in Rows()) bodies.Add(Create(r));
        Sun = bodies[0].transform;
        ApplyScale();

        if (orbit != null)
        {
            orbit.target = Sun;
            orbit.distance = realScale ? 5f : 150f;
        }
        Select(bodies[0]);
        if (orbit != null) orbit.distance = realScale ? 5f : 150f;   // overview, not a close-up
    }

    PlanetInfo Create(Row r)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // includes a collider for clicking
        go.name = r.name;
        go.transform.SetParent(transform, false);

        var rend = go.GetComponent<Renderer>();
        if (r.distanceMkm == 0f && sunMaterial != null) rend.sharedMaterial = sunMaterial;
        else rend.material.color = r.color;

        var p = go.AddComponent<PlanetInfo>();
        p.planetName = r.name;
        p.info = r.info;
        p.diameterKm = r.diameterKm;
        p.distanceMkm = r.distanceMkm;
        p.periodDays = r.periodDays;
        p.rotationHours = r.rotationHours;
        return p;
    }

    float Distance(PlanetInfo p)
    {
        // Real: 1 unit = 1 million km. Visible: compressed so everything fits on screen.
        return realScale ? p.distanceMkm : 12f + Mathf.Sqrt(p.distanceMkm) * 3f;
    }

    float Size(PlanetInfo p)
    {
        if (realScale) return p.diameterKm / 1000000f;
        if (p.distanceMkm == 0f) return 10f;
        return 0.6f + Mathf.Sqrt(p.diameterKm / 1000f) * 0.25f;
    }

    void ApplyScale()
    {
        foreach (var p in bodies) p.transform.localScale = Vector3.one * Size(p);
    }

    // --- Hooks for UI -------------------------------------------------------
    public void ToggleScale()
    {
        realScale = !realScale;
        ApplyScale();
        if (orbit != null && orbit.target != null) orbit.Focus(orbit.target);
    }

    public void SetSpeed(float daysPerSec)
    {
        daysPerSecond = daysPerSec;
    }

    public void Select(PlanetInfo p)
    {
        if (orbit != null) orbit.Focus(p.transform);
        if (Selected != null) Selected(p);
    }
    // ------------------------------------------------------------------------

    void Update()
    {
        simDays += Time.deltaTime * daysPerSecond;

        foreach (var p in bodies)
        {
            if (p.periodDays > 0f)
            {
                float a = simDays / p.periodDays * Mathf.PI * 2f;
                p.transform.position = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Distance(p);
            }
            if (p.rotationHours != 0f)
            {
                p.transform.Rotate(Vector3.up, 360f / p.rotationHours * spinSpeed * Time.deltaTime, Space.Self);
            }
        }

        // Keys 0-8 jump to Sun and planets (handy in true-to-scale mode, where planets are tiny).
        var kb = Keyboard.current;
        if (kb != null)
        {
            for (int i = 0; i < FocusKeys.Length && i < bodies.Count; i++)
            {
                if (kb[FocusKeys[i]].wasPressedThisFrame) Select(bodies[i]);
            }
        }
    }

    void LateUpdate()
    {
        // Aim the directional light from the Sun towards the planet being viewed,
        // so its sun-facing side is lit (a simple stand-in for a point light at the Sun).
        if (sunLight == null || orbit == null || orbit.target == null || Sun == null) return;
        if (orbit.target == Sun) return;

        Vector3 dir = orbit.target.position - Sun.position;
        if (dir.sqrMagnitude > 1e-6f) sunLight.transform.rotation = Quaternion.LookRotation(dir);
    }
}
