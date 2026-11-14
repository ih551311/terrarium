using UnityEditor;
using UnityEngine;

namespace DynamicWeatherSystem.Editor
{
    /// <summary>Adds a "Test Strike" button to the Lightning Module in Play Mode.</summary>
    [CustomEditor(typeof(LightningModule))]
    public class LightningModuleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Test Strike", GUILayout.Height(24)))
                    ((LightningModule)target).Strike();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to test a strike. Automatic strikes happen " +
                    "when the active state has Lightning Frequency above 0.", MessageType.None);
        }
    }
}
