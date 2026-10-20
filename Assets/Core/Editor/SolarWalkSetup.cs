using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

// One-click project setup. In Unity: menu  Solar Walk > Set Up Project
//  - desktop scene: adds the HUD, render modes, planet labels and orbit lines
//  - creates the AR "Marker" scene (table-top) and the AR "Walk" scene (room-scale)
//  - main menu: adds a title and a "Desktop Mode" button
//  - puts Main, desktop, Marker and Walk in the Build Profiles scene list
// Safe to run more than once (Marker and Walk are rebuilt each time).
public static class SolarWalkSetup
{
    const string MainScene    = "Assets/Core/Scenes/Main.unity";
    const string DesktopScene = "Assets/Core/Scenes/desktop.unity";
    const string MarkerScene  = "Assets/Planets/Scenes/Marker.unity";
    const string WalkScene    = "Assets/Walk/Scenes/Walk.unity";
    const string SunMatPath   = "Assets/Core/SunMat.mat";
    const string LineMatPath  = "Assets/Core/OrbitLine.mat";
    const string ARRigPrefab  = "Assets/Samples/XR Interaction Toolkit/3.5.1/AR Starter Assets/Prefabs/XR Origin (AR Rig).prefab";

    [MenuItem("Solar Walk/Set Up Project")]
    public static void SetUp()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var sun = AssetDatabase.LoadAssetAtPath<Material>(SunMatPath);
        var line = EnsureLineMaterial();

        FixDesktopScene(sun, line);
        if (!BuildARScene(MarkerScene, false, sun, line)) return;
        if (!BuildARScene(WalkScene, true, sun, line)) return;
        FixMainMenu();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScene, true),
            new EditorBuildSettingsScene(DesktopScene, true),
            new EditorBuildSettingsScene(MarkerScene, true),
            new EditorBuildSettingsScene(WalkScene, true),
        };
        AssetDatabase.SaveAssets();

        EditorSceneManager.OpenScene(MainScene);
        EditorUtility.DisplayDialog("Solar Walk",
            "Done!\n\n" +
            "- desktop scene: UI, labels, orbit lines added\n" +
            "- Marker (AR table) and Walk (AR room-scale) scenes created\n" +
            "- Main menu: Marker / Walk / Desktop buttons\n" +
            "- Build scene list: Main, desktop, Marker, Walk\n\n" +
            "Press Play in the Main scene to try it.", "OK");
    }

    // ------------------------------------------------------------------ assets

    static Material EnsureLineMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(LineMatPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        mat = new Material(shader);
        var c = new Color(0.45f, 0.55f, 0.85f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        AssetDatabase.CreateAsset(mat, LineMatPath);
        return mat;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static T FindOrCreate<T>(string name) where T : Component
    {
        var c = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        return c != null ? c : new GameObject(name).AddComponent<T>();
    }

    // ----------------------------------------------------------------- desktop

    static void FixDesktopScene(Material sun, Material line)
    {
        var scene = EditorSceneManager.OpenScene(DesktopScene, OpenSceneMode.Single);

        var solar = Object.FindFirstObjectByType<SolarSystem>();
        if (solar == null)
        {
            Debug.LogError("Solar Walk setup: no SolarSystem found in the desktop scene.");
            return;
        }
        if (solar.sunMaterial == null) solar.sunMaterial = sun;

        Camera cam = null;
        var orbitCam = Object.FindFirstObjectByType<OrbitCamera>();
        if (orbitCam != null) cam = orbitCam.GetComponent<Camera>();
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

        var labels = GetOrAdd<PlanetLabels>(solar.gameObject);
        labels.solar = solar;
        labels.cam = cam;

        var orbits = GetOrAdd<OrbitLines>(solar.gameObject);
        orbits.solar = solar;
        orbits.lineMaterial = line;

        var rm = FindOrCreate<RenderModes>("RenderModes");

        var hud = FindOrCreate<SolarHUD>("HUD");
        hud.mode = SolarHUD.Mode.Desktop;
        hud.solar = solar;
        hud.cam = cam;
        hud.renderModes = rm;

        EditorUtility.SetDirty(solar);
        EditorUtility.SetDirty(labels);
        EditorUtility.SetDirty(orbits);
        EditorUtility.SetDirty(rm);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // -------------------------------------------------------------- AR scenes

    static bool BuildARScene(string path, bool walk, Material sun, Material line)
    {
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARRigPrefab);
        if (rigPrefab == null)
        {
            EditorUtility.DisplayDialog("Solar Walk",
                "Could not find the AR rig prefab:\n" + ARRigPrefab +
                "\n\nImport it from Package Manager > XR Interaction Toolkit > Samples > AR Starter Assets.", "OK");
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // AR Session (one per scene) + AR camera rig from the AR Starter Assets sample
        var session = new GameObject("AR Session");
        session.AddComponent<ARSession>();
        session.AddComponent<ARInputManager>();

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
        var cam = rig.GetComponentInChildren<Camera>(true);
        var raycaster = rig.GetComponentInChildren<ARRaycastManager>(true);
        if (raycaster == null) raycaster = rig.AddComponent<ARRaycastManager>();
        var planes = rig.GetComponentInChildren<ARPlaneManager>(true);
        if (planes == null) planes = rig.AddComponent<ARPlaneManager>();

        // The solar system (starts hidden; shown when the user taps a surface)
        var root = new GameObject("Solar Root");
        var solar = root.AddComponent<SolarSystem>();
        solar.sunMaterial = sun;
        solar.daysPerSecond = walk ? 2f : 20f;
        solar.ambient = new Color(0.3f, 0.3f, 0.35f);   // brighter than space so planets read well over the camera image

        var labels = root.AddComponent<PlanetLabels>();
        labels.solar = solar;
        labels.cam = cam;

        var orbits = root.AddComponent<OrbitLines>();
        orbits.solar = solar;
        orbits.lineMaterial = line;
        root.SetActive(false);

        // Preview camera for the Editor / PC (disabled; only used when AR is not available)
        var fly = cam.gameObject.AddComponent<FlyCamera>();
        fly.solar = solar;
        fly.enabled = false;

        // Placement + UI
        var placer = new GameObject("AR Controller").AddComponent<ARSolarPlacer>();
        placer.walkMode = walk;
        placer.solar = solar;
        placer.cam = cam;
        placer.raycaster = raycaster;
        placer.planes = planes;
        placer.fly = fly;
        placer.startScale = walk ? 0.03f : 0.003f;

        var hud = new GameObject("HUD").AddComponent<SolarHUD>();
        hud.mode = walk ? SolarHUD.Mode.ARWalk : SolarHUD.Mode.ARTable;
        hud.solar = solar;
        hud.cam = cam;
        hud.placer = placer;
        placer.hud = hud;

        EditorSceneManager.SaveScene(scene, path);
        return true;
    }

    // --------------------------------------------------------------- main menu

    static void FixMainMenu()
    {
        var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        var menu = Object.FindFirstObjectByType<MainMenu>();
        var marker = GameObject.Find("MarkerButton");
        var walk = GameObject.Find("WalkButton");
        if (menu == null || marker == null || walk == null)
        {
            Debug.LogWarning("Solar Walk setup: main menu not as expected, skipped the Desktop button.");
            return;
        }

        var desktop = GameObject.Find("DesktopButton");
        if (desktop == null)
        {
            desktop = Object.Instantiate(walk, walk.transform.parent);
            desktop.name = "DesktopButton";

            var btn = desktop.GetComponent<Button>();
            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(btn.onClick, i);
            UnityEventTools.AddPersistentListener(btn.onClick, menu.LoadDesktop);

            var label = desktop.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "Desktop Mode";
                label.gameObject.name = "Desktop Mode";
            }
        }

        // Stack the three buttons: Marker, Walk, Desktop
        ((RectTransform)marker.transform).anchoredPosition = new Vector2(0f, 300f);
        ((RectTransform)walk.transform).anchoredPosition = new Vector2(0f, 60f);
        ((RectTransform)desktop.transform).anchoredPosition = new Vector2(0f, -180f);

        // Title
        if (GameObject.Find("Title") == null)
        {
            var title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(walk.transform.parent, false);
            var t = title.AddComponent<TextMeshProUGUI>();
            t.text = "SOLAR WALK";
            t.fontSize = 110;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(1f, 0.85f, 0.35f);
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -200f);
            rt.sizeDelta = new Vector2(1000f, 180f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
