# Quick Start Guide — URP Dynamic Weather System

**Get up and running in under 5 minutes.**

---

## Step 1 — Import the Package

Import the `.unitypackage` via **Assets > Import Package > Custom Package**.

Make sure your project already has the **Universal Render Pipeline** package installed (`Window > Package Manager > Universal RP`).

---

## Fastest Way (1 click) — new in 1.1

1. **GameObject > Dynamic Weather System > Weather System**
   (or **Weather System + Cycle** if you want the weather to change on its own).
2. Press **Play**. Select `WeatherSystem` and use **Play Mode Preview** in the Inspector to try each preset.
3. Done — skip to [Step 5](#step-5--change-weather-from-code) to control it from code.

The rest of this guide explains how to build the same setup by hand.

> **Already using 1.0?** Select your `WeatherSystem` and click **Add Missing Modules** in the Inspector to get Snow, Wind and Lightning.

---

## Step 2 — Set Up the WeatherSystem Hierarchy (manual)

Create the following GameObject hierarchy in your scene:

```
WeatherSystem          ← WeatherManager component
├── LightModule        ← LightModule component
├── FogModule          ← FogModule component
├── RainModule         ← RainModule component
├── SkyModule          ← SkyModule component
├── AudioModule        ← AudioModule component
├── SnowModule         ← SnowModule component      (optional, 1.1)
├── WindModule         ← WindModule component      (optional, 1.1)
└── LightningModule    ← LightningModule component (optional, 1.1)
```

**How to build it:**

1. Create an empty GameObject: `GameObject > Create Empty` → rename it `WeatherSystem`.
2. Add the `WeatherManager` component: **Add Component > Dynamic Weather System > Weather Manager**.
3. For each module you want, create a child GameObject under `WeatherSystem` and add the corresponding component:
   - **Add Component > Dynamic Weather System > Modules > Light Module**
   - **Add Component > Dynamic Weather System > Modules > Fog Module**
   - **Add Component > Dynamic Weather System > Modules > Rain Module**
   - **Add Component > Dynamic Weather System > Modules > Sky Module**
   - **Add Component > Dynamic Weather System > Modules > Audio Module**

> You don't need all five modules. Add only the ones relevant to your project.

---

## Step 3 — Generate Preset Assets

In the **Project window**, select the folder where you want to store the weather presets (e.g. `Assets/WeatherPresets`), then:

`Assets > Create > Dynamic Weather System > Create Sample Presets`

This creates four ready-to-use assets:

| Asset | Description |
|---|---|
| `WS_Clear.asset` | Bright sun, no fog, no rain |
| `WS_Rain.asset` | Overcast light, light fog, rain intensity 0.6 |
| `WS_Fog.asset` | Dim light, dense fog, no rain |
| `WS_Storm.asset` | Dark sky, heavy fog, rain intensity 0.95 |

> The presets ship with `ambientClip = null`. Assign audio clips in the Inspector after creation.
> Ready-to-use `.wav` loops are included in `DynamicWeatherSystem/Audio/`.

---

## Step 4 — Assign the Initial State

Select the `WeatherSystem` GameObject. In the **WeatherManager** Inspector:

1. Drag one of the preset assets (e.g. `WS_Clear`) to the **Initial State** slot.
2. Optionally adjust the **Default Transition Duration** (default: 2 seconds).

The system will apply the initial state immediately when the scene starts.

---

## Step 5 — Change Weather from Code

```csharp
using UnityEngine;
using DynamicWeatherSystem;

public class GameWeatherController : MonoBehaviour
{
    [SerializeField] private WeatherManager weatherManager;

    [SerializeField] private WeatherStateData clearPreset;
    [SerializeField] private WeatherStateData rainPreset;
    [SerializeField] private WeatherStateData stormPreset;

    // Smooth transition using the default duration set in WeatherManager
    public void SetRain()   => weatherManager.SetWeather(rainPreset);

    // Smooth transition with a custom duration
    public void SetStorm()  => weatherManager.SetWeather(stormPreset, 5f);

    // Immediate change — no transition
    public void SetClear()  => weatherManager.SetWeather(clearPreset, 0f);
}
```

Assign `WeatherManager` and the preset assets in the Inspector.

---

## Step 6 — Play Mode Test

Press **Play**. The initial state is applied immediately. You can also use the **custom Inspector** on `WeatherManager` to trigger transitions without writing code:

1. Select the `WeatherSystem` GameObject.
2. In the **WeatherManager** Inspector, select a state from the dropdown.
3. Click **Apply with Transition** or **Apply Immediate**.

---

## Optional — Add the Demo UI

If you want the built-in on-screen controls:

1. Create an empty GameObject in your scene → rename it `WeatherDemoUI`.
2. Add the `WeatherDemoUI` component (**Add Component > Dynamic Weather System > Demo > Weather Demo UI**).
3. In the Inspector:
   - Assign the `WeatherManager` reference (or leave it blank — it auto-finds one in the scene).
   - Assign the four preset assets (`presetClear`, `presetRain`, `presetFog`, `presetStorm`).
4. Press **Play** — a control panel appears in the top-left corner of the Game view.

---

## Minimum Setup (Light + Fog only)

Not every project needs rain or audio. The smallest useful setup is:

```
WeatherSystem
├── LightModule
└── FogModule
```

Modules not present in the hierarchy are simply skipped. There are no errors for absent modules.

---

## Next Steps

- [API Reference](API_Reference.md) — full `WeatherManager` API and property details
- [Custom Modules](Custom_Modules.md) — how to extend the system with your own effects
- [README](README.md) — full feature overview and FAQ
