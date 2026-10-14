using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Left-click a planet to select it and show its info in the side panel.
public class PlanetPicker : MonoBehaviour
{
    public Camera cam;
    public SolarSystem solar;
    public TMP_Text title;
    public TMP_Text body;

    void OnEnable()
    {
        if (solar != null) solar.Selected += Show;
    }

    void OnDisable()
    {
        if (solar != null) solar.Selected -= Show;
    }

    void Update()
    {
        var m = Mouse.current;
        if (m == null || cam == null || solar == null) return;
        if (!m.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = cam.ScreenPointToRay(m.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var p = hit.collider.GetComponentInParent<PlanetInfo>();
            if (p != null) solar.Select(p);
        }
    }

    void Show(PlanetInfo p)
    {
        if (title != null) title.text = p.planetName;
        if (body == null) return;

        string stats;
        if (p.distanceMkm > 0f)
        {
            string retro = p.rotationHours < 0f ? " (retrograde)" : "";
            stats = "\n\nDiameter: " + p.diameterKm.ToString("N0") + " km"
                  + "\nDistance from Sun: " + p.distanceMkm.ToString("N1") + " million km"
                  + "\nOrbital period: " + p.periodDays.ToString("N0") + " days ("
                  + (p.periodDays / 365.25f).ToString("N1") + " years)"
                  + "\nRotation: " + Mathf.Abs(p.rotationHours).ToString("N1") + " hours" + retro;
        }
        else
        {
            stats = "\n\nDiameter: " + p.diameterKm.ToString("N0") + " km";
        }
        body.text = p.info + stats;
    }
}
