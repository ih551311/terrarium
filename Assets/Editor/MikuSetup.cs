using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 햄부기(Assets/Models/Miku/MIKU.fbx)를 게임에 넣는다.
///  1) 색 재질 다섯 개(피부, 머리, 옷, 눈, 입)를 만든다. FBX에는 피부색과 흰색밖에 없어서 부분별로 색을 입힌다.
///  2) 꾸밈 재료용 작은 모델 Assets/Models/Deco/deco_miku.prefab (키 8cm, 병 안에 놓는다)
///  3) NPC용 Assets/Prefabs/Miku.prefab (키 1.7m, NpcTalk, 충돌 캡슐)
///  4) 원래 섬(Island)의 집 앞에 NPC를 세운다 (이름 MikuNPC)
/// 메뉴: 유리병 정원 > 햄부기 만들기. 여러 번 눌러도 같은 결과가 나온다. (재료 데이터와 아이콘은 재료 데이터 만들기, 재료 아이콘 만들기로)
/// </summary>
public static class MikuSetup
{
    private const float HairGrow = 1.07f; // 머리카락 덮개를 키우는 배율
    private const string FbxPath = "Assets/Models/Miku/MIKU.fbx";
    private const string MatFolder = "Assets/Models/Miku/Materials";
    private const string DecoPrefabPath = "Assets/Models/Deco/deco_miku.prefab";
    private const string NpcPrefabPath = "Assets/Prefabs/Miku.prefab";
    private const string IslandPath = "Assets/Scenes/Island.unity";

    private static Material Mat(string name, Color color, float smooth)
    {
        Directory.CreateDirectory(MatFolder);
        string path = MatFolder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>FBX를 키 targetHeight(미터)로, 발이 y=0에 오고 얼굴이 +z를 보게 감싼 오브젝트를 만든다 (장면에 임시로 놓인다).</summary>
    private static GameObject Wrap(string name, float targetHeight, bool faceMinusZ = false)
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        Material skin = Mat("MikuSkin", new Color(0.98f, 0.84f, 0.77f), 0.25f);
        Material hair = Mat("MikuHair", new Color(0.18f, 0.78f, 0.80f), 0.35f);
        Material dress = Mat("MikuDress", new Color(0.22f, 0.25f, 0.31f), 0.3f);
        Material eye = Mat("MikuEye", new Color(0.06f, 0.30f, 0.38f), 0.6f);
        Material mouth = Mat("MikuMouth", new Color(0.86f, 0.38f, 0.45f), 0.3f);

        GameObject root = new GameObject(name);
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        inst.name = "Model";
        inst.transform.SetParent(root.transform, false);
        inst.transform.localRotation = Quaternion.Euler(0f, faceMinusZ ? 0f : 180f, 0f); // 모델은 -z를 보고 있다. NPC는 +z를 보게 돌리고, 꾸밈 재료는 아이콘에서 얼굴이 보이게 -z 그대로 둔다

        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
        {
            Material m;
            switch (r.name)
            {
                case "Box001": case "d": m = skin; break;                 // 얼굴, 몸(팔, 다리, 목)
                case "Box003": case "Box004": case "Cone001": m = hair; break; // 양갈래 머리와 앞머리
                case "Box005": m = dress; break;                           // 옷
                case "Box008": m = mouth; break;                           // 입
                default: m = eye; break;                                   // Sphere002, Sphere003: 눈
            }
            r.sharedMaterial = m;
        }

        // 머리카락 덮개(Cone001)가 얼굴(Box001)보다 조금 작아서 얼굴이 군데군데 삐져나와 "탈모"처럼 보인다. 덮개를 키워서 덮는다.
        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
        {
            if (r.name != "Cone001") continue;
            Bounds hb = r.bounds;
            Vector3 before = hb.center;
            r.transform.localScale *= HairGrow;
            Vector3 after = r.bounds.center;
            r.transform.position += before - after; // 키우기 전의 가운데를 그대로 둔다
        }

        Bounds b = BoundsOf(inst);
        float s = targetHeight / b.size.y;
        inst.transform.localScale = Vector3.one * s;
        b = BoundsOf(inst);
        inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
        return root;
    }

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    [MenuItem("유리병 정원/햄부기 만들기")]
    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath) == null) return "햄부기 모델을 찾지 못했어요: " + FbxPath;
        EditorSceneManager.SaveOpenScenes();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != IslandPath)
            EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);

        // 1) 꾸밈 재료용 작은 모델
        GameObject deco = Wrap("deco_miku", 0.08f, true);
        PrefabUtility.SaveAsPrefabAsset(deco, DecoPrefabPath);
        Object.DestroyImmediate(deco);

        // 2) NPC 프리팹
        Directory.CreateDirectory("Assets/Prefabs");
        GameObject npc = Wrap("Miku", 1.7f);
        CapsuleCollider cap = npc.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 0.85f, 0f);
        cap.radius = 0.4f;
        cap.height = 1.7f;
        NpcTalk talk = npc.AddComponent<NpcTalk>();
        SerializedObject so = new SerializedObject(talk);
        so.FindProperty("npcName").stringValue = "햄부기";
        SerializedProperty lines = so.FindProperty("lines");
        lines.arraySize = 1;
        lines.GetArrayElementAtIndex(0).stringValue = "안녕 나는 햄부기야";
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(npc, NpcPrefabPath);
        Object.DestroyImmediate(npc);

        // 3) 집 앞에 세운다: 시작할 때 바라보는 쪽, 집 앞 도착 지점(FromHut)에서 오른쪽 2.5m, 앞으로 4m
        GameObject old = GameObject.Find("MikuNPC");
        while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find("MikuNPC"); }
        SpawnPoint hut = null;
        foreach (SpawnPoint sp in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)) if (sp.Id == "FromHut") hut = sp;
        if (hut == null) return "집 앞 도착 지점(FromHut)을 찾지 못했어요.";
        Terrain terrain = Terrain.activeTerrain;
        Vector3 pos = hut.transform.position + new Vector3(2.5f, 0f, -4f);
        pos.y = terrain != null ? terrain.SampleHeight(pos) + terrain.transform.position.y : hut.transform.position.y;
        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
        placed.name = "MikuNPC";
        placed.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(hut.transform.position - pos));

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "햄부기를 만들었어요: NPC 위치 " + pos + ", 꾸밈 모델 " + DecoPrefabPath;
    }
}
