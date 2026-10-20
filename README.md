# Solar Walk (AR/VR)

A solar system with real planet data in three modes:

| Mode | Runs on | What it does |
|---|---|---|
| **Desktop** | Laptop | 3D solar system. Click a planet for info. Right-drag rotates, scroll zooms. Buttons switch render mode (solid/wireframe), lighting and real/visible scale. |
| **Marker** (AR table) | Android phone | Scan a table, then tap to place the solar system on it. Pinch to resize, tap a planet for info. |
| **Walk** (AR room-scale) | Android phone | The system is about 13 m across. Walk between the planets; the screen shows how far away the selected planet is. |

On the laptop, Marker and Walk open in a preview mode: WASD to move, right-drag to look, click a planet to fly to it.

## Setup (Windows)

1. Install **Unity Hub**, then editor **6000.0.44f1** (open `unityhub://6000.0.44f1/101c91f3a8fb` in a browser). Tick **Android Build Support** with **OpenJDK** and **Android SDK & NDK Tools**.
2. Install [Git for Windows](https://git-scm.com/download/win) (it includes Git LFS), then in a terminal:
   ```
   cd C:\
   mkdir Projects
   cd Projects
   git lfs install
   git clone https://github.com/BeTechSavvy/SOLAR-WALK-ARVR.git
   ```
   Use `git clone`, not "Download ZIP". The ZIP leaves the LFS images as empty placeholders.
3. Unity Hub → **Add → Add project from disk** → `C:\Projects\SOLAR-WALK-ARVR`. Open it with **6000.0.44f1**. If it offers to upgrade the project, cancel. The first import takes 10–20 min.
4. In Unity: menu **Solar Walk → Set Up Project**, then click OK.
5. Open `Assets/Core/Scenes/Main.unity` and press **Play**.

## Build to an Android phone

1. On the phone, enable **Developer options** and **USB debugging**, then connect it by USB and allow the PC.
2. Unity → **File → Build Profiles** → **Android** → **Switch Platform**.
3. Check that the scene list is Main, desktop, Marker, Walk (the setup menu sets this).
4. **Build And Run**, save as `SolarWalk.apk`. The first build takes 20–40 min.
5. The phone needs **Google Play Services for AR** (ARCore). The app prompts you to install it if it's missing.

## Scripts (Assets/Core/Scripts)

- `SolarSystem.cs` builds the Sun and 8 planets from real data. Orbital and rotation periods keep their real ratios.
- `OrbitCamera.cs` is the desktop camera (mouse and touch).
- `RenderModes.cs` switches between solid and wireframe rendering (W) and toggles lighting (L).
- `SolarHUD.cs` builds the on-screen UI in code, and handles tap/click planet selection and the info panel.
- `ARSolarPlacer.cs` handles AR plane detection, places the system with a tap (raycast against planes), and resizes it with a pinch.
- `FlyCamera.cs` is the first-person preview camera used when AR isn't available.
- `PlanetLabels.cs` draws planet name labels that always face the camera. `OrbitLines.cs` draws the orbit rings.
- `Editor/SolarWalkSetup.cs` adds the **Solar Walk → Set Up Project** menu, which creates the scenes and fixes the build list.
