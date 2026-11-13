using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 병 꾸미기에 필요한 공용 재료를 만든다: 재료 데이터의 병 안 기본 도형, 바닥재 모래 재료,
/// 놓을 자리 미리보기용 반투명 재질, 섬의 시험용 모래.
/// (병 자체는 JarSetup이 만든다.)
/// 메뉴: 유리병 정원 > 병 설정. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class BottleSetup
{
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string SandPath = "Assets/Data/Materials/sand.asset";

    [MenuItem("유리병 정원/병 설정")]
    public static string Build()
    {
        SetPlaceholder("pinecone", PlaceholderShape.Sphere, new Color(0.55f, 0.35f, 0.2f), new Vector3(0.07f, 0.07f, 0.07f));
        SetPlaceholder("twig", PlaceholderShape.Capsule, new Color(0.7f, 0.55f, 0.35f), new Vector3(0.025f, 0.05f, 0.025f));
        EnsureSandAsset();
        GetOrCreateGhostMaterial();

        // 섬에는 시험용 모래를 둔다 (바닷가를 만들기 전까지 임시)
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        // 씬을 열면 메모리의 데이터가 정리될 수 있어서, 여기서 다시 불러온다.
        AddTestSand(EnsureSandAsset());
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "병 설정 완료";
    }

    /// <summary>바닥재 모래 재료(데이터). 없으면 만든다. 흙은 항상 쓸 수 있어서 재료 데이터가 필요 없다.</summary>
    public static MaterialData EnsureSandAsset()
    {
        MaterialData sand = AssetDatabase.LoadAssetAtPath<MaterialData>(SandPath);
        if (sand != null) return sand;

        sand = ScriptableObject.CreateInstance<MaterialData>();
        sand.id = "sand";
        sand.displayName = "모래";
        sand.kind = MaterialKind.Ground;
        sand.placeholderColor = new Color(0.87f, 0.78f, 0.55f);
        AssetDatabase.CreateAsset(sand, SandPath);
        return sand;
    }

    private static void AddTestSand(MaterialData sand)
    {
        GameObject root = GameObject.Find("TestMaterials");
        if (root == null) return;

        // 이미 있으면 재료 연결만 다시 해 준다.
        if (GameObject.Find("Sand0") != null)
        {
            foreach (string n in new[] { "Sand0", "Sand1" })
            {
                GameObject existing = GameObject.Find(n);
                if (existing != null) UISetup.SetRef(existing.GetComponent<Pickup>(), "material", sand);
            }
            return;
        }

        Material mat = GetOrCreateMaterial("SandTan", sand.placeholderColor);
        for (int i = 0; i < 2; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Sand" + i;
            go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(-3.5f - i * 1.2f, 0.12f, 6f);
            go.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Pickup pickup = go.AddComponent<Pickup>();
            UISetup.SetRef(pickup, "material", sand);
        }
    }

    public static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = "Assets/Data/Placeholder/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    /// <summary>놓을 자리 미리보기에 쓰는 반투명(Transparent) 재질</summary>
    public static Material GetOrCreateGhostMaterial()
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
