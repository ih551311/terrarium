# Changelog — URP Dynamic Weather System

## 1.1.0

### New
- **Snow Module** — particle snow with flutter and wind drift. Generates its own soft flake material: no textures needed.
- **Snow preset** (`WS_Snow`).
- **Lightning Module** — random flashes with delayed thunder. Includes a procedural thunder sound, so it works without audio files. `Strike()` API, `Struck` event and a **Test Strike** inspector button.
- **Wind Module** — drives a Unity WindZone (trees, vegetation, particles with External Forces). Exposes `WindVector` for gameplay code.
- **Wind for rain and snow** — drops and flakes tilt and drift with each state's `windStrength` / `windDirection`.
- **Weather Cycle** — automatic weather changes (sequential or weighted random), no code required.
- **Events** — `WeatherChangeStarted` and `WeatherChanged` (C#) plus matching UnityEvents in the Inspector.
- **One-click setup** — `GameObject > Dynamic Weather System > Weather System` (and `+ Cycle`).
- **Add Missing Modules** button and a module list in the `WeatherManager` Inspector.
- **Welcome window** — `Tools > Dynamic Weather System > Welcome`. Opens once per update and can be turned off.
- `WeatherManager.GetModule<T>()`, `Modules`, `RefreshModules()`, `DefaultTransitionDuration`.
- `WeatherManager` now appears under `Add Component > Dynamic Weather System`.
- The Storm preset now has wind and lightning. The demo scene has a Snow button.

### Fixed
- **Rain transparency** — the runtime rain material now uses real alpha blending, so `rainColor` alpha works as documented (before, drops were rendered opaque).
- Interrupting a transition several times in a row no longer reads from a snapshot scheduled for destruction.
- `LightModule` warns only once when no Directional Light is assigned (before, every frame during transitions). It also auto-finds the light at runtime if the field is empty.

### Changed
- Transition logs are **off by default**. Enable **Log Transitions** on `WeatherManager` to see them.
- `LightModule` has a new **Control Ambient Light** option (on by default = same behaviour as 1.0). Turn it off to keep your scene's own ambient lighting.

### Upgrading from 1.0.x
- **Your scenes, prefabs and weather states keep working.** No fields were renamed or removed, and every new field defaults to "off" (no snow, no wind, no lightning).
- To use the new effects in an existing scene, select your `WeatherSystem` and click **Add Missing Modules** in the Inspector.
- **If you edited the included presets** (`Presets/WS_*.asset`), note that importing the update replaces them. To keep your changes, untick those files in the import dialog, or duplicate your presets before updating. Only `WS_Storm` changed (wind + lightning added) and `WS_Snow` is new.
- If you call `SetWeather` from code, nothing changes. The new events are optional.
- The demo scene now has its own unique asset ID (1.0 shared one with Unity's template SampleScene, which could clash with your own scenes). If after updating you see a second copy such as `Scene WeatherSystem 1.unity`, you can delete the older demo scene.

## 1.0.0
- Initial release: Light, Fog, Rain, Sky and Audio modules, 4 presets, interruptible transitions, custom inspector and demo scene.
