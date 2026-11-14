# URP Dynamic Weather System — Documentation

**Version 1.1.0 | Unity 2022.3+ | Universal Render Pipeline only**

> **Updating from 1.0.x?** See [CHANGELOG.md](CHANGELOG.md#upgrading-from-10x). Your scenes and weather states keep working as before.

---

## Table of Contents

1. [Overview](#overview)
2. [Requirements](#requirements)
3. [Package Contents](#package-contents)
4. [Quick Start (5 minutes)](#quick-start)
5. [Modules Reference](#modules-reference)
6. [API Reference](#api-reference)
7. [Creating Custom Modules](#creating-custom-modules)
8. [Weather State Data Fields](#weather-state-data-fields)
9. [Editor Tools](#editor-tools)
10. [Demo Scene](#demo-scene)
11. [FAQ](#faq)

---

## Overview

**URP Dynamic Weather System** is a modular, drop-in weather framework for Unity's Universal Render Pipeline.

It lets you define weather states as ScriptableObject assets, then blend smoothly between them at runtime — with full control over directional light, ambient light, fog, rain, snow, wind, lightning, skybox, and ambient audio. Transitions can be interrupted at any time without visual popping.

**Included weather states:** Clear · Rain · Fog · Storm · Snow

---

## Requirements

| Requirement | Version |
|---|---|
| Unity | 2022.3 LTS or newer |
| Universal Render Pipeline | Any URP version compatible with your Unity version |
| Render Pipeline | **URP only** — Built-in and HDRP are not supported |

No other packages or dependencies are required.

---

## Package Contents

```
DynamicWeatherSystem/
├── Runtime/
│   ├── Core/
│   │   ├── WeatherStateData.cs     — ScriptableObject: all weather parameters
│   │   ├── WeatherManager.cs       — Central orchestrator (+ events)
│   │   ├── WeatherModule.cs        — Abstract base for all modules
│   │   ├── WeatherCycle.cs         — Automatic weather changes over time
│   │   └── WeatherParticleUtility.cs — Internal helpers for particle modules
│   ├── Modules/
│   │   ├── LightModule.cs          — Directional light + ambient color
│   │   ├── FogModule.cs            — Scene fog (Exponential Squared)
│   │   ├── RainModule.cs           — Particle-based rain
│   │   ├── SkyModule.cs            — Skybox tint + exposure
│   │   ├── AudioModule.cs          — Ambient audio crossfade
│   │   ├── SnowModule.cs           — Particle-based snow
│   │   ├── WindModule.cs           — Drives a WindZone (trees, particles)
│   │   └── LightningModule.cs      — Lightning flashes + thunder
│   └── Demo/
│       └── WeatherDemoUI.cs        — Runtime demo UI (OnGUI, no Canvas needed)
├── Editor/
│   ├── WeatherManagerEditor.cs     — Custom inspector with live preview
│   ├── WeatherPresetsCreator.cs    — One-click preset asset generator
│   ├── WeatherSystemSetup.cs       — One-click scene setup / Add Missing Modules
│   ├── WeatherWelcomeWindow.cs     — Welcome window (Tools > Dynamic Weather System)
│   └── LightningModuleEditor.cs    — "Test Strike" button
├── Presets/
│   ├── WS_Clear.asset
│   ├── WS_Rain.asset
│   ├── WS_Fog.asset
│   ├── WS_Storm.asset
│   └── WS_Snow.asset
├── Audio/
│   ├── low_rain_loop.wav
│   ├── storm-wind-loop.wav
│   └── wind_loop.wav
├── Example/
│   └── Scene WeatherSystem.unity   — Demo scene
└── Documentation/
    ├── README.md                   — This file
    ├── QuickStart.md
    ├── API_Reference.md
    ├── Custom_Modules.md
    └── CHANGELOG.md
```

---

## Quick Start

See **[QuickStart.md](QuickStart.md)** for the full step-by-step setup guide.

**TL;DR — 3 steps:**

1. `GameObject > Dynamic Weather System > Weather System` (or **Weather System + Cycle** for automatic weather).
2. Press Play and try the presets from the **Play Mode Preview** section of the `WeatherManager` Inspector.
3. Change the weather from code: `weatherManager.SetWeather(myState, 5f);`

Prefer to build it by hand? Add `WeatherManager` to an empty GameObject and add the modules you need as child GameObjects — see [QuickStart.md](QuickStart.md).

---

## Modules Reference

Each module is a `MonoBehaviour` that must be a **child** of the `WeatherManager` GameObject. The manager auto-discovers all children on `Awake`.

### LightModule
Controls the scene's directional light color/intensity and ambient light color.
- Auto-finds the first Directional Light in the scene if none is assigned.
- Set the light reference manually in the Inspector for multi-light scenes.

### FogModule
Controls Unity's built-in scene fog (`RenderSettings`).
- Uses **Exponential Squared** mode for smooth distance-based falloff.
- Fog is kept active during a transition if either the source or target state has fog enabled, preventing visual popping.

### RainModule
Particle-based rain that follows the main camera.
- Auto-creates and configures a `ParticleSystem` if none is assigned.
- Rain particles use world-space simulation and stretch rendering for a realistic look.
- Set `followTarget` in the Inspector to override the default `Camera.main` tracking.
- Controlled by `rainIntensity`, `rainColor`, and `rainSpeed` on `WeatherStateData`.

### SkyModule
Adjusts the active skybox material's tint and exposure at runtime.
- Creates a **material instance** at startup — your original skybox asset is never modified.
- If the skybox material doesn't have `_Tint`/`_SkyTint` or `_Exposure` properties, a warning is logged and the module is skipped gracefully.
- Requires a Procedural or custom skybox material; plain color backgrounds are not affected.

### AudioModule
Crossfades between two ambient audio clips synced to the transition duration.
- Uses two `AudioSource` components created as children automatically.
- If source and target states share the same clip, only the volume is interpolated (no restart).
- Assign `AudioClip` assets to each `WeatherStateData` preset in the Inspector.
- Use `masterVolume` on the component to globally attenuate all weather audio.

### SnowModule *(new in 1.1)*
Particle-based snow that follows the main camera, with gentle flutter.
- Auto-creates its `ParticleSystem` and a soft snowflake material — no textures needed.
- Optional `snowMaterial` for a custom look.
- Controlled by `snowIntensity`, `snowColor` and `snowSpeed`. Flakes drift with the state's wind.

### WindModule *(new in 1.1)*
Drives a Unity `WindZone` (created automatically if none is assigned).
- Moves terrain trees, SpeedTree vegetation and particle systems that use **External Forces**.
- Controlled by `windStrength` (0–1) and `windDirection` (degrees).
- Read `WindVector`, `Strength` and `Direction` from gameplay code (flags, cloth, boats...).
- Rain and snow tilt with the wind even without this module.

### LightningModule *(new in 1.1)*
Random lightning flashes with delayed thunder.
- Uses its own flash light, so it never conflicts with `LightModule`.
- Controlled by `lightningFrequency` (strikes per minute).
- Assign your own `thunderClips`, or leave them empty to use the built-in procedural thunder.
- Call `Strike()` to trigger one manually; subscribe to `Struck` to react (camera shake, etc.).
- The Inspector has a **Test Strike** button in Play Mode.

### WeatherCycle *(new in 1.1)*
Changes the weather automatically over time. Not a module: add it to any GameObject.
- **Sequential** (in order, looping) or **Random** (weighted, optional no-repeat).
- Each entry has its own minimum and maximum duration.
- `Play()`, `Pause()`, `Resume()`, `Next()` from code.

---

## API Reference

See **[API_Reference.md](API_Reference.md)** for the full reference.

### WeatherManager — Public API

```csharp
// Change weather using the default transition duration (set in Inspector)
weatherManager.SetWeather(WeatherStateData state);

// Change weather with a specific duration in seconds
weatherManager.SetWeather(WeatherStateData state, float duration);

// Immediate change (no transition)
weatherManager.SetWeather(state, 0f);

// Read current state
WeatherStateData current = weatherManager.CurrentState;

// Check if a transition is running
bool transitioning = weatherManager.IsTransitioning;

// Transition progress [0, 1], eased by AnimationCurve
float progress = weatherManager.TransitionProgress;

// Events (new in 1.1) — also available as UnityEvents in the Inspector
weatherManager.WeatherChangeStarted += (from, to) => Debug.Log("Changing to " + to.stateName);
weatherManager.WeatherChanged       += state => Debug.Log("Weather is now " + state.stateName);

// Get a module (new in 1.1)
var lightning = weatherManager.GetModule<LightningModule>();
```

---

## Creating Custom Modules

See **[Custom_Modules.md](Custom_Modules.md)** for a full example with code.

**Summary:** Inherit from `WeatherModule`, implement `Blend(from, to, t)`, and add your component as a child of the `WeatherManager` GameObject.

```csharp
public class MyModule : WeatherModule
{
    public override void Blend(WeatherStateData from, WeatherStateData to, float t)
    {
        // t = 0 → pure "from" state
        // t = 1 → pure "to" state
        float value = Mathf.Lerp(from.someField, to.someField, t);
        // apply value to your system
    }
}
```

---

## Weather State Data Fields

| Field | Type | Range | Description |
|---|---|---|---|
| `stateName` | string | — | Display name used in logs and inspector |
| `fogEnabled` | bool | — | Enables scene fog for this state |
| `fogColor` | Color | — | Fog color |
| `fogDensity` | float | 0–0.1 | Fog density (Exponential Squared) |
| `lightColor` | Color | — | Directional light color |
| `lightIntensity` | float | 0–8 | Directional light intensity |
| `ambientColor` | Color | — | Scene ambient light color |
| `skyboxTint` | Color | — | Multiplicative tint on the skybox |
| `skyboxExposure` | float | 0–8 | Skybox brightness multiplier |
| `ambientClip` | AudioClip | — | Looping ambient audio clip |
| `ambientVolume` | float | 0–1 | Target volume for this state |
| `rainIntensity` | float | 0–1 | 0 = no rain, 1 = maximum rain |
| `rainColor` | Color | — | Rain particle color (alpha = opacity) |
| `rainSpeed` | float | 2–20 | Downward fall speed in m/s |
| `snowIntensity` | float | 0–1 | 0 = no snow, 1 = heavy snowfall *(1.1)* |
| `snowColor` | Color | — | Snowflake color (alpha = opacity) *(1.1)* |
| `snowSpeed` | float | 0.3–5 | Snowflake fall speed in m/s *(1.1)* |
| `windStrength` | float | 0–1 | 0 = calm, 1 = storm wind *(1.1)* |
| `windDirection` | float | 0–360 | Direction the wind blows towards, in degrees *(1.1)* |
| `lightningFrequency` | float | 0–30 | Lightning strikes per minute, 0 = off *(1.1)* |

All fields added in 1.1 default to "off", so states created with 1.0 look exactly the same.

---

## Editor Tools

### Welcome Window
`Tools > Dynamic Weather System > Welcome` — quick start, what's new and links. It opens once after each package update; disable that with the toggle at the bottom.

### One-Click Setup
- `GameObject > Dynamic Weather System > Weather System` — creates a complete WeatherSystem with every module.
- `GameObject > Dynamic Weather System > Weather System + Cycle` — same, plus a pre-filled `WeatherCycle`.
- **Add Missing Modules** button in the `WeatherManager` Inspector — adds any built-in module your setup doesn't have yet (handy after updating).

### Custom Inspector (WeatherManager)
Select the `WeatherManager` component to access:
- **Modules** — lists the modules this manager drives.
- **Weather State** dropdown — lists all `WeatherStateData` assets in the project.
- **Transition Duration** — override duration for the next preview transition.
- **Apply with Transition** — triggers a live transition in Play Mode.
- **Apply Immediate** — applies the state instantly with no fade.
- **Progress bar** — shows active transition progress, live-updated in the Inspector.

### Preset Creator
`Assets > Create > Dynamic Weather System > Create Sample Presets`

Creates all five preset assets (WS_Clear, WS_Rain, WS_Fog, WS_Storm, WS_Snow) in the currently selected project folder. If the assets already exist, their values are updated without duplicating them.

### Creating a New Weather State
`Right-click in Project window > Create > Dynamic Weather System > Weather State`

---

## Demo Scene

Open `Example/Scene WeatherSystem.unity`. The scene includes:
- A fully configured `WeatherSystem` hierarchy (Manager + all modules)
- A `WeatherDemoUI` component providing an in-game panel with:
  - Preset buttons with color-coded accents (Clear, Rain, Fog, Storm, Snow)
  - Transition duration slider
  - Live transition progress bar

No Canvas or Input System package is required.

---

## FAQ

**Q: The rain particles don't appear.**
A: The `RainModule` auto-creates a `ParticleSystem`, but needs a URP-compatible particle material. If your project's URP version doesn't include `Particles/Unlit`, create a `ParticleSystem` manually, assign a valid URP material to it, then assign the `ParticleSystem` reference on `RainModule` in the Inspector.

**Q: The skybox doesn't change.**
A: Check the Console for warnings from `SkyModule`. Your skybox material must expose `_Tint` or `_SkyTint` and `_Exposure` properties. Standard URP skyboxes (Procedural, Six Sided) support these. Shader Graph skyboxes may require adding those properties manually.

**Q: Audio clips aren't playing.**
A: Make sure the scene has an `AudioModule` and that the state's `ambientClip` is assigned. Presets made with the Preset Creator have no clip: assign one of the `.wav` files in the `Audio/` folder.

**Q: My scene's ambient lighting changes when the weather starts.**
A: `LightModule` drives the ambient color by default. Untick **Control Ambient Light** on the `LightModule` to keep your scene's own ambient settings (Skybox/Gradient ambient, baked lighting).

**Q: Lightning doesn't light up my scene.**
A: The flash uses an additional Directional Light. In your URP Asset, make sure **Additional Lights** is set to *Per Pixel* (or *Per Vertex*).

**Q: Too many logs in the Console.**
A: Transition logs are off by default since 1.1. Enable **Log Transitions** on `WeatherManager` when debugging.

**Q: Can I use this with Built-in or HDRP?**
A: Not in V1. The fog, light, and skybox APIs target URP. HDRP support is planned for a future version.

**Q: Can I add more weather states beyond the four included?**
A: Yes. Create a new `WeatherStateData` asset, fill in the values, and pass it to `SetWeather()`. There is no limit on the number of states.

**Q: What happens if I call SetWeather during an active transition?**
A: The current transition is stopped and a runtime snapshot is created capturing the exact visual state at that moment. The new transition starts from that snapshot — no visual jump occurs.

---

*URP Dynamic Weather System v1.1.0 — © 2024 All rights reserved.*
