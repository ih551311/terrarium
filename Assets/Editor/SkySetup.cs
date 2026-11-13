using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬 씬에 낮과 밤의 하늘을 만든다.
/// - 낮 하늘: AllSky 팩의 Cartoon Base BlueSky, 밤 하늘: Cartoon Base NightSky (둘 다 큐브맵)
/// - 두 하늘을 섞는 재질(Assets/Data/Placeholder/SkyBlend.mat)을 만들고, 섬 씬에 SkyController를 둔다.
/// 메뉴: 유리병 정원 > 하늘 만들기. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class SkySetup
{
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string MaterialPath = "Assets/Data/Placeholder/SkyBlend.mat";
    private const string DayMaterialPath = "Assets/AllSkyFree/Cartoon Base BlueSky/Day_BlueSky_Nothing Equirect.mat";
    private const string NightMaterialPath = "Assets/AllSkyFree/Cartoon Base NightSky/Cartoon Base NightSky Equirect.mat";

    [MenuItem("유리병 정원/하늘 만들기")]
    public static string Build()
    {
        Material day = AssetDatabase.LoadAssetAtPath<Material>(DayMaterialPath);
        Material night = AssetDatabase.LoadAssetAtPath<Material>(NightMaterialPath);
        if (day == null || night == null) return "AllSky 팩의 Cartoon Base BlueSky / NightSky를 찾을 수 없어요.";
        Shader shader = Shader.Find("유리병 정원/Sky Blend");
        if (shader == null) return "하늘 섞기 셰이더를 찾을 수 없어요.";

        // 1) 섞는 재질
        Directory.CreateDirectory("Assets/Data/Placeholder");
        Material blend = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (blend == null)
        {
            blend = new Material(shader);
            AssetDatabase.CreateAsset(blend, MaterialPath);
        }
        blend.shader = shader;
        blend.SetTexture("_TexA", day.GetTexture("_Tex"));
        blend.SetTexture("_TexB", night.GetTexture("_Tex"));
        blend.SetFloat("_Blend", 0f);
        EditorUtility.SetDirty(blend);

        // 2) 섬 씬에 하늘 컨트롤러
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);

        GameObject old = GameObject.Find("SkyController");
        while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find("SkyController"); }

        Light sun = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) sun = l;
        if (sun == null) return "섬 씬에 방향 조명(해)이 없어요.";

        GameObject go = new GameObject("SkyController");
        SkyController controller = go.AddComponent<SkyController>();
        UISetup.SetRef(controller, "sun", sun);
        UISetup.SetRef(controller, "skyMaterial", blend);

        // 편집 화면에서도 낮 하늘이 보이게 맞춰 둔다 (실행하면 SkyController가 시간에 맞춰 바꾼다)
        RenderSettings.skybox = blend;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "하늘 만들기 완료: 낮 " + day.name + ", 밤 " + night.name;
    }
}
