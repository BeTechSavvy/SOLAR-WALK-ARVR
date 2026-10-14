using UnityEngine;

// Data for one body (Sun or planet). Attached automatically by SolarSystem.
// Teammates' planet prefabs can also use this component later.
public class PlanetInfo : MonoBehaviour
{
    public string planetName;
    [TextArea] public string info;
    public float diameterKm;
    public float distanceMkm;     // million km from the Sun (0 for the Sun)
    public float periodDays;      // orbital period in Earth days (0 = does not orbit)
    public float rotationHours;   // sidereal rotation period; negative = retrograde
}
