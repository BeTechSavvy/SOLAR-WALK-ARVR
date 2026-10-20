using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// On-screen UI shared by all three modes (Desktop, AR table, AR walk).
// The whole UI is built in code at start-up, so nothing has to be wired by hand.
//  - Info panel (top-left): name, description and real data of the selected body.
//  - Button bar (bottom): Menu, Speed, Next planet, plus mode-specific buttons.
//  - Hint line: tells the user what to do next.
// Tapping/clicking a planet selects it (works with mouse and touch).
public class SolarHUD : MonoBehaviour
{
    public enum Mode { Desktop, ARTable, ARWalk }

    [Header("Assign in Inspector (Set Up Project does this)")]
    public Mode mode = Mode.Desktop;
    public SolarSystem solar;
    public Camera cam;
    public RenderModes renderModes;      // Desktop only (optional)
    public ARSolarPlacer placer;         // AR scenes only (optional)

    /// <summary>A tap/click that did not hit a planet or the UI. Used by ARSolarPlacer to place the system.</summary>
    public event Action<Vector2> TapOnWorld;

    static readonly float[] Speeds = { 2f, 20f, 100f, 0f };   // simulated days per second
    int speedIndex;

    PlanetInfo selected;
    TMP_Text titleText, bodyText, hintText, statsText, speedLabel;
    GameObject infoPanel;
    Button moveButton, scaleButton;
    string hint = "";
    bool fallback;

    readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    Vector2 downPos;
    bool tracking, downOverUI;

    // ------------------------------------------------------------------ setup

    void Awake()
    {
        if (solar == null) solar = FindFirstObjectByType<SolarSystem>(FindObjectsInactive.Include);
        if (cam == null) cam = Camera.main;
        if (solar != null) solar.Selected += OnSelected;

        EnsureEventSystem();
        BuildUI();

        if (renderModes != null) renderModes.statsLabel = statsText;
        else statsText.gameObject.SetActive(false);

        speedIndex = mode == Mode.ARWalk ? 0 : 1;
        ApplySpeed();
        SetHint(mode == Mode.Desktop
            ? "Click a planet for info  |  right-drag to rotate  |  scroll to zoom  |  keys 0-8 jump to a body"
            : "");
    }

    void OnDestroy()
    {
        if (solar != null) solar.Selected -= OnSelected;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("HUD Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var root = canvasGO.transform;

        // --- Info panel (top-left)
        infoPanel = Panel("Info Panel", root, new Color(0.03f, 0.05f, 0.12f, 0.82f));
        var prt = (RectTransform)infoPanel.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(16f, -16f);
        prt.sizeDelta = new Vector2(500f, 0f);
        var vlg = infoPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 14, 16);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        infoPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = Label("Title", infoPanel.transform, 34, FontStyles.Bold, new Color(1f, 0.85f, 0.35f));
        bodyText = Label("Body", infoPanel.transform, 19, FontStyles.Normal, new Color(0.9f, 0.92f, 1f));
        titleText.text = "Solar Walk";
        bodyText.text = "Select a planet.";

        // --- Render stats (top-right, desktop only)
        statsText = Label("Render Stats", root, 17, FontStyles.Normal, new Color(0.75f, 0.9f, 1f));
        var srt = statsText.rectTransform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1f, 1f);
        srt.anchoredPosition = new Vector2(-16f, -16f);
        srt.sizeDelta = new Vector2(460f, 140f);
        statsText.alignment = TextAlignmentOptions.TopRight;

        // --- Button bar (bottom)
        var bar = new GameObject("Button Bar", typeof(RectTransform));
        bar.transform.SetParent(root, false);
        var brt = (RectTransform)bar.transform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 12f);
        brt.sizeDelta = new Vector2(-24f, 64f);
        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        MakeButton(bar.transform, "Menu", GoToMenu);
        speedLabel = MakeButton(bar.transform, "Speed", CycleSpeed).GetComponentInChildren<TMP_Text>();
        MakeButton(bar.transform, "Next", NextBody);
        if (mode == Mode.Desktop)
        {
            scaleButton = MakeButton(bar.transform, "Real scale", ToggleScale);
            if (renderModes != null)
            {
                MakeButton(bar.transform, "Render [W]", () => renderModes.CycleMode());
                MakeButton(bar.transform, "Light [L]", () => renderModes.ToggleAmbient());
            }
        }
        else
        {
            moveButton = MakeButton(bar.transform, "Re-place", () => { if (placer != null) placer.ResetPlacement(); });
        }
        MakeButton(bar.transform, "Info", () => infoPanel.SetActive(!infoPanel.activeSelf));

        // --- Hint line (just above the bar)
        hintText = Label("Hint", root, 20, FontStyles.Italic, Color.white);
        var hrt = hintText.rectTransform;
        hrt.anchorMin = new Vector2(0f, 0f);
        hrt.anchorMax = new Vector2(1f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 84f);
        hrt.sizeDelta = new Vector2(-40f, 60f);
        hintText.alignment = TextAlignmentOptions.Bottom;
        hintText.raycastTarget = false;
    }

    static GameObject Panel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TMP_Text Label(string name, Transform parent, float size, FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
    {
        var go = Panel(text + " Button", parent, new Color(0.12f, 0.2f, 0.42f, 0.9f));
        var b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        var colors = b.colors;
        colors.highlightedColor = new Color(0.8f, 0.9f, 1f);
        colors.pressedColor = new Color(0.6f, 0.7f, 1f);
        b.colors = colors;
        b.onClick.AddListener(onClick);

        var t = Label("Text", go.transform, 22, FontStyles.Bold, Color.white);
        t.text = text;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12;
        t.fontSizeMax = 22;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6f, 4f);
        rt.offsetMax = new Vector2(-6f, -4f);
        return b;
    }

    // ------------------------------------------------------------ public API

    public void SetHint(string text)
    {
        hint = text ?? "";
    }

    // Called by ARSolarPlacer when AR is not available (e.g. in the Unity Editor).
    public void SetFallback(bool on)
    {
        fallback = on;
        if (moveButton != null) moveButton.gameObject.SetActive(!on);
    }

    // ---------------------------------------------------------------- buttons

    void GoToMenu()
    {
        SceneManager.LoadScene("Main");
    }

    void CycleSpeed()
    {
        speedIndex = (speedIndex + 1) % Speeds.Length;
        ApplySpeed();
    }

    void ApplySpeed()
    {
        float s = Speeds[speedIndex];
        if (solar != null) solar.SetSpeed(s);
        if (speedLabel != null) speedLabel.text = s <= 0f ? "Paused" : "Speed " + s.ToString("0") + " d/s";
    }

    void NextBody()
    {
        if (solar == null || !solar.Ready) return;
        var list = solar.Bodies;
        int i = 0;
        for (int k = 0; k < list.Count; k++) if (list[k] == selected) i = k + 1;
        solar.Select(list[i % list.Count]);
    }

    void ToggleScale()
    {
        if (solar == null) return;
        solar.ToggleScale();
        if (scaleButton != null)
            scaleButton.GetComponentInChildren<TMP_Text>().text = solar.realScale ? "Visible scale" : "Real scale";
    }

    // --------------------------------------------------------------- picking

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) GoToMenu();   // also the Android back button

        HandleTap();
        UpdateHint();
    }

    void HandleTap()
    {
        var ptr = Pointer.current;
        if (ptr == null) return;

        if (ptr.press.wasPressedThisFrame)
        {
            downPos = ptr.position.ReadValue();
            downOverUI = OverUI(downPos);
            tracking = true;
        }

        if (tracking && ptr.press.wasReleasedThisFrame)
        {
            tracking = false;
            Vector2 up = ptr.position.ReadValue();
            if (downOverUI || (up - downPos).sqrMagnitude > 30f * 30f) return;   // UI click or a drag

            var p = PickPlanet(up);
            if (p != null) solar.Select(p);
            else TapOnWorld?.Invoke(up);
        }
    }

    bool OverUI(Vector2 pos)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var ped = new PointerEventData(es) { position = pos };
        uiHits.Clear();
        es.RaycastAll(ped, uiHits);
        return uiHits.Count > 0;
    }

    PlanetInfo PickPlanet(Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || solar == null || !solar.Ready || !solar.isActiveAndEnabled) return null;

        // 1) exact hit on a planet's collider
        var hits = Physics.RaycastAll(cam.ScreenPointToRay(screenPos), 100000f);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var p = h.collider.GetComponentInParent<PlanetInfo>();
            if (p != null) return p;
        }

        // 2) forgiving: nearest planet on screen within a finger's width (planets can be tiny in AR)
        float best = Mathf.Max(48f, Screen.dpi > 0 ? Screen.dpi * 0.3f : 48f);
        PlanetInfo pick = null;
        foreach (var p in solar.Bodies)
        {
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position);
            if (sp.z <= 0f) continue;
            float d = Vector2.Distance(screenPos, sp);
            if (d < best) { best = d; pick = p; }
        }
        return pick;
    }

    // ------------------------------------------------------------- info panel

    void OnSelected(PlanetInfo p)
    {
        selected = p;
        titleText.text = p.planetName;

        string stats;
        if (p.distanceMkm > 0f)
        {
            string retro = p.rotationHours < 0f ? " (retrograde)" : "";
            stats = "\n\n<b>Diameter:</b> " + p.diameterKm.ToString("N0") + " km"
                  + "\n<b>Distance from Sun:</b> " + p.distanceMkm.ToString("N1") + " million km"
                  + "\n<b>Orbital period:</b> " + p.periodDays.ToString("N0") + " days ("
                  + (p.periodDays / 365.25f).ToString("N1") + " years)"
                  + "\n<b>Rotation:</b> " + Mathf.Abs(p.rotationHours).ToString("N1") + " hours" + retro;
        }
        else
        {
            stats = "\n\n<b>Diameter:</b> " + p.diameterKm.ToString("N0") + " km";
        }
        bodyText.text = p.info + stats;
        infoPanel.SetActive(true);
    }

    void UpdateHint()
    {
        string extra = "";
        bool walkLike = mode == Mode.ARWalk || (fallback && mode != Mode.Desktop);
        if (walkLike && selected != null && cam != null && solar != null && solar.isActiveAndEnabled)
        {
            float d = Vector3.Distance(cam.transform.position, selected.transform.position);
            extra = (hint.Length > 0 ? "\n" : "") + "<b>" + selected.planetName + "</b> is " + d.ToString("0.0") + " m away";
        }
        hintText.text = hint + extra;
    }
}
