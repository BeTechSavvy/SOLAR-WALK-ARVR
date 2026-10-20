using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Floating name labels above the Sun and planets.
// Labels always face the camera and keep the same size on screen at any distance/scale.
// The selected body's label is highlighted. Put this on the SolarSystem object.
public class PlanetLabels : MonoBehaviour
{
    public SolarSystem solar;
    public Camera cam;
    public float screenSize = 0.04f;   // label height as a fraction of the distance to the camera
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.85f, 0.3f);

    class Entry { public PlanetInfo body; public TextMeshPro text; }
    readonly List<Entry> labels = new List<Entry>();
    PlanetInfo selected;

    void Awake()
    {
        if (solar == null) solar = GetComponent<SolarSystem>();
        if (solar != null) solar.Selected += OnSelected;
    }

    void OnDestroy()
    {
        if (solar != null) solar.Selected -= OnSelected;
    }

    void OnSelected(PlanetInfo p)
    {
        selected = p;
    }

    void Build()
    {
        foreach (var p in solar.Bodies)
        {
            var go = new GameObject(p.planetName + " Label");
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TextMeshPro>();
            t.text = p.planetName;
            t.fontSize = 10f;                       // about 1 unit tall before scaling
            t.alignment = TextAlignmentOptions.Bottom;
            t.rectTransform.sizeDelta = new Vector2(40f, 2f);
            t.rectTransform.pivot = new Vector2(0.5f, 0f);   // text sits on top of the anchor point
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(0, 0, 0, 255);
            labels.Add(new Entry { body = p, text = t });
        }
    }

    void LateUpdate()
    {
        if (solar == null) return;
        if (labels.Count == 0)
        {
            if (!solar.Ready) return;
            Build();
        }

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        var camT = cam.transform;
        float parentScale = Mathf.Max(transform.lossyScale.x, 1e-6f);

        foreach (var e in labels)
        {
            var body = e.body.transform;
            float radius = body.lossyScale.x * 0.5f;
            float dist = Vector3.Distance(camT.position, body.position);
            float h = dist * screenSize * (cam.fieldOfView / 60f);

            var lt = e.text.transform;
            lt.position = body.position + camT.up * (radius + h * 0.3f);
            lt.rotation = camT.rotation;
            lt.localScale = Vector3.one * (h / parentScale);
            e.text.color = e.body == selected ? selectedColor : normalColor;
        }
    }
}
