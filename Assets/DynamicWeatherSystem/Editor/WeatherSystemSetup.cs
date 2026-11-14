using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DynamicWeatherSystem.Editor
{
    /// <summary>
    /// One-click scene setup for the Dynamic Weather System.
    ///
    ///   GameObject > Dynamic Weather System > Weather System
    ///   GameObject > Dynamic Weather System > Weather System + Cycle
    ///   Tools > Dynamic Weather System > Create Weather System
    ///
    /// Also used by the WeatherManager inspector to add missing modules to existing setups.
    /// </summary>
    public static class WeatherSystemSetup
    {
        private const string GameObjectMenu = "GameObject/Dynamic Weather System/";
        private const string ToolsMenu      = "Tools/Dynamic Weather System/";

        /// <summary>Built-in modules, in the order they are created.</summary>
        public static readonly Type[] BuiltInModules =
        {
            typeof(LightModule),
            typeof(SkyModule),
            typeof(FogModule),
            typeof(RainModule),
            typeof(SnowModule),
            typeof(WindModule),
            typeof(LightningModule),
            typeof(AudioModule),
        };

        // Preset asset names shipped with the package.
        private static readonly string[] PresetNames =
            { "WS_Clear", "WS_Rain", "WS_Fog", "WS_Storm", "WS_Snow" };

        [MenuItem(GameObjectMenu + "Weather System", false, 10)]
        private static void CreateFromGameObjectMenu(MenuCommand command)
            => CreateWeatherSystem(command.context as GameObject, withCycle: false);

        [MenuItem(GameObjectMenu + "Weather System + Cycle", false, 11)]
        private static void CreateWithCycleFromGameObjectMenu(MenuCommand command)
            => CreateWeatherSystem(command.context as GameObject, withCycle: true);

        [MenuItem(ToolsMenu + "Create Weather System", false, 20)]
        private static void CreateFromToolsMenu()
            => CreateWeatherSystem(null, withCycle: false);

        /// <summary>Creates a complete WeatherSystem hierarchy in the active scene.</summary>
        public static WeatherManager CreateWeatherSystem(GameObject parent, bool withCycle)
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<WeatherManager>();
            if (existing != null &&
                !EditorUtility.DisplayDialog("Dynamic Weather System",
                    "This scene already has a WeatherManager on '" + existing.name + "'.\n\n" +
                    "Create another one anyway?", "Create", "Cancel"))
            {
                Selection.activeGameObject = existing.gameObject;
                return existing;
            }

            var root = new GameObject("WeatherSystem");
            Undo.RegisterCreatedObjectUndo(root, "Create Weather System");
            if (parent != null)
                GameObjectUtility.SetParentAndAlign(root, parent);

            var manager = Undo.AddComponent<WeatherManager>(root);

            foreach (var moduleType in BuiltInModules)
                AddModule(root.transform, moduleType);

            var clear = FindPreset("WS_Clear");
            if (clear != null)
            {
                var so = new SerializedObject(manager);
                so.FindProperty("initialState").objectReferenceValue = clear;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (withCycle)
                AddCycle(manager);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return manager;
        }

        /// <summary>Returns the built-in module types not yet present under the manager.</summary>
        public static List<Type> GetMissingModules(WeatherManager manager)
        {
            var missing = new List<Type>();
            foreach (var type in BuiltInModules)
            {
                if (manager.GetComponentInChildren(type, true) == null)
                    missing.Add(type);
            }
            return missing;
        }

        /// <summary>Adds each missing built-in module as a child GameObject. Undoable.</summary>
        public static void AddMissingModules(WeatherManager manager)
        {
            var missing = GetMissingModules(manager);
            if (missing.Count == 0) return;

            Undo.SetCurrentGroupName("Add Missing Weather Modules");
            int group = Undo.GetCurrentGroup();

            foreach (var type in missing)
                AddModule(manager.transform, type);

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Adds a WeatherCycle pre-filled with the package presets.</summary>
        public static WeatherCycle AddCycle(WeatherManager manager)
        {
            var cycle = manager.GetComponent<WeatherCycle>();
            if (cycle == null)
                cycle = Undo.AddComponent<WeatherCycle>(manager.gameObject);

            var so      = new SerializedObject(cycle);
            var entries = so.FindProperty("entries");

            if (entries.arraySize == 0)
            {
                // Clear is the most common state; snow is excluded by default
                // because it rarely mixes with rain in the same scene.
                AddEntry(entries, FindPreset("WS_Clear"), 3f, 90f, 180f);
                AddEntry(entries, FindPreset("WS_Rain"),  2f, 60f, 120f);
                AddEntry(entries, FindPreset("WS_Fog"),   1f, 45f, 90f);
                AddEntry(entries, FindPreset("WS_Storm"), 1f, 40f, 80f);
            }

            so.ApplyModifiedProperties();
            return cycle;
        }

        // --- Helpers ---

        private static void AddModule(Transform parent, Type moduleType)
        {
            var go = new GameObject(moduleType.Name);
            Undo.RegisterCreatedObjectUndo(go, "Add " + moduleType.Name);
            Undo.SetTransformParent(go.transform, parent, "Add " + moduleType.Name);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var component = Undo.AddComponent(go, moduleType);

            if (component is LightModule)
            {
                var light = LightModule.FindDirectionalLight();
                if (light != null)
                {
                    var so = new SerializedObject(component);
                    so.FindProperty("directionalLight").objectReferenceValue = light;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void AddEntry(SerializedProperty entries, WeatherStateData state,
                                     float weight, float min, float max)
        {
            if (state == null) return;

            int i = entries.arraySize;
            entries.InsertArrayElementAtIndex(i);
            var e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("state").objectReferenceValue = state;
            e.FindPropertyRelative("weight").floatValue      = weight;
            e.FindPropertyRelative("minDuration").floatValue = min;
            e.FindPropertyRelative("maxDuration").floatValue = max;
        }

        /// <summary>Finds a preset by asset name, preferring the copy inside this package.</summary>
        public static WeatherStateData FindPreset(string assetName)
        {
            WeatherStateData fallback = null;

            foreach (var guid in AssetDatabase.FindAssets(assetName + " t:WeatherStateData"))
            {
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<WeatherStateData>(path);
                if (asset == null || asset.name != assetName) continue;

                if (path.Contains("/DynamicWeatherSystem/")) return asset;
                if (fallback == null) fallback = asset;
            }
            return fallback;
        }

        /// <summary>All package presets that exist in the project.</summary>
        public static List<WeatherStateData> FindAllPresets()
        {
            var list = new List<WeatherStateData>();
            foreach (var n in PresetNames)
            {
                var p = FindPreset(n);
                if (p != null) list.Add(p);
            }
            return list;
        }
    }
}
