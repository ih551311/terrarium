using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 집 안 씬의 병에 BottleContents를 붙이고, 재료 데이터에 병 안 모습(기본 도형)을 정한다.
/// 메뉴: 유리병 정원 > 병 설정. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class BottleSetup
{
    private const string InteriorPath = "Assets/Scenes/HutInterior.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";

    [MenuItem("유리병 정원/병 설정")]
    public static string Build()
    {
        SetPlaceholder("pinecone", PlaceholderShape.Sphere, new Color(0.55f, 0.35f, 0.2f), new Vector3(0.07f, 0.07f, 0.07f));
        SetPlaceholder("twig", PlaceholderShape.Capsule, new Color(0.7f, 0.55f, 0.35f), new Vector3(0.025f, 0.05f, 0.025f));

        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(InteriorPath, OpenSceneMode.Single);

        GameObject bottle = GameObject.Find("Bottle");
        if (bottle == null) return "집 안 씬에서 Bottle을 찾을 수 없어요.";
        BottleContents contents = bottle.GetComponent<BottleContents>();
        if (contents == null) contents = bottle.AddComponent<BottleContents>();
        UISetup.SetRef(contents, "ghostMaterial", GetOrCreateGhostMaterial());

        // 코드로 컴포넌트를 붙이면 씬이 "변경됨"으로 표시되지 않아서, 직접 표시해야 저장된다.
        EditorSceneManager.MarkSceneDirty(bottle.scene);
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        return "병 설정 완료";
    }

    /// <summary>놓을 자리 미리보기에 쓰는 반투명(Transparent) 재질</summary>
    private static Material GetOrCreateGhostMaterial()
    {
        const string path = "Assets/Data/Placeholder/GhostPreview.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.5f));
        m.SetFloat("_Surface", 1f);    // Transparent
        m.SetFloat("_Blend", 0f);      // Alpha
        m.SetFloat("_SrcBlend", 5f);   // SrcAlpha
        m.SetFloat("_DstBlend", 10f);  // OneMinusSrcAlpha
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static void SetPlaceholder(string id, PlaceholderShape shape, Color color, Vector3 size)
    {
        MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>("Assets/Data/Materials/" + id + ".asset");
        if (data == null) return;
        data.placeholderShape = shape;
        data.placeholderColor = color;
        data.placeholderSize = size;
        EditorUtility.SetDirty(data);
    }
}
