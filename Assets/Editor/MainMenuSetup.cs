using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 화면 씬(Assets/Scenes/MainMenu.unity)을 만들고, 빌드 설정의 맨 앞에 넣는다.
/// 씬에는 카메라(소리 듣는 귀 포함), EventSystem, MainMenu 스크립트, 그리고 화면 캔버스(MainMenuCanvas)가 들어 있다. 캔버스는 UIBake가 만든다.
/// 다시 눌러도 같은 씬을 새로 만든다. 지금 열려 있는 씬은 건드리지 않는다.
/// </summary>
public static class MainMenuSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("유리병 정원/메인 화면 만들기")]
    public static string Build()
    {
        if (EditorApplication.isPlaying) return "Play 중에는 만들 수 없어요";

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cam = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = cam.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.17f, 0.22f);
        camera.cullingMask = 0; // 3D는 그리지 않는다 (화면은 전부 UI)
        cam.AddComponent<AudioListener>();

        GameObject events = new GameObject("EventSystem");
        events.AddComponent<EventSystem>();
        events.AddComponent<InputSystemUIInputModule>();

        MainMenu menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        UIBake.BuildMainMenuCanvas(menu); // 화면 전체를 실제 캔버스 오브젝트로 만든다

        EditorSceneManager.SaveScene(scene, ScenePath);

        // 빌드 설정: 메인 화면이 맨 앞(0번)
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.path != ScenePath) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        return "메인 화면 씬을 만들었어요: " + ScenePath;
    }
}
