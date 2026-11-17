using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 치비(Assets/Models/Chibi/Chibi.fbx, 인사하는 자세의 SD 캐릭터)를 게임에 넣는다.
/// FBX는 뼈대가 있는 모델(SkinnedMeshRenderer 15개)이라, 자세 그대로 굳힌 일반 메쉬(Assets/Models/Chibi/Baked)로 바꿔서 쓴다.
/// (병 속에 놓는 재료는 일반 메쉬여야 크기를 잴 수 있고 가볍다)
///  1) Assets/Models/Chibi/Chibi_Baked.prefab: 굳힌 모델 (키 그대로)
///  2) 꾸밈 재료용 작은 모델 Assets/Models/Deco/deco_chibi.prefab (키 9cm)
///  3) NPC용 Assets/Prefabs/Chibi.prefab (키 1.6m, NpcTalk, 충돌 캡슐)
///  4) 원래 섬(Island)의 집 앞, 시작 자리의 왼쪽 앞에 NPC를 세운다 (이름 ChibiNPC. 오른쪽은 미쿠)
/// 메뉴: 유리병 정원 > 치비 만들기. 여러 번 눌러도 같은 결과가 나온다. (재료 데이터와 아이콘은 재료 데이터 만들기, 재료 아이콘 만들기로)
/// </summary>
public static class ChibiSetup
{
    private const string FbxPath = "Assets/Models/Chibi/Chibi.fbx";
    private const string BakedFolder = "Assets/Models/Chibi/Baked";
    private const string BakedPrefabPath = "Assets/Models/Chibi/Chibi_Baked.prefab";
    private const string DecoPrefabPath = "Assets/Models/Deco/deco_chibi.prefab";
    private const string NpcPrefabPath = "Assets/Prefabs/Chibi.prefab";
    private const string IslandPath = "Assets/Scenes/Island.unity";

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>FBX의 모든 SkinnedMeshRenderer를 지금 자세 그대로 일반 메쉬로 굳혀 프리팹으로 저장한다.</summary>
    private static void Bake()
    {
        Directory.CreateDirectory(BakedFolder);
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        GameObject root = new GameObject("Chibi_Baked");

        foreach (SkinnedMeshRenderer smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh mesh = new Mesh { name = smr.name };
            smr.BakeMesh(mesh, false); // 렌더러 안쪽 좌표의 메쉬. 크기는 아래에서 오브젝트 크기(lossyScale)로 준다
            string meshPath = BakedFolder + "/" + smr.name.Replace('.', '_') + ".asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            GameObject part = new GameObject(smr.name);
            part.transform.SetParent(root.transform, false);
            part.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            part.transform.localScale = smr.transform.lossyScale; // 팔찌, 별, 리본 같은 부품은 크기가 따로 있다
            part.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            part.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
        }
        Object.DestroyImmediate(inst);
        PrefabUtility.SaveAsPrefabAsset(root, BakedPrefabPath);
        Object.DestroyImmediate(root);
    }

    /// <summary>굳힌 모델을 키 targetHeight(미터)로, 발이 y=0에 오게 감싼 오브젝트를 만든다 (장면에 임시로 놓인다). 모델은 +z를 본다.</summary>
    private static GameObject Wrap(string name, float targetHeight)
    {
        GameObject root = new GameObject(name);
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BakedPrefabPath));
        inst.name = "Model";
        inst.transform.SetParent(root.transform, false);
        Bounds b = BoundsOf(inst);
        float s = targetHeight / b.size.y;
        inst.transform.localScale = Vector3.one * s;
        b = BoundsOf(inst);
        inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
        return root;
    }

    [MenuItem("유리병 정원/치비 만들기")]
    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath) == null) return "치비 모델을 찾지 못했어요: " + FbxPath;
        EditorSceneManager.SaveOpenScenes();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != IslandPath)
            EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);

        Bake();

        GameObject deco = Wrap("deco_chibi", 0.09f);
        PrefabUtility.SaveAsPrefabAsset(deco, DecoPrefabPath);
        Object.DestroyImmediate(deco);

        Directory.CreateDirectory("Assets/Prefabs");
        GameObject npc = Wrap("Chibi", 1.6f);
        CapsuleCollider cap = npc.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 0.8f, 0f);
        cap.radius = 0.4f;
        cap.height = 1.6f;
        NpcTalk talk = npc.AddComponent<NpcTalk>();
        SerializedObject so = new SerializedObject(talk);
        so.FindProperty("npcName").stringValue = "치비";
        SerializedProperty lines = so.FindProperty("lines");
        lines.arraySize = 1;
        lines.GetArrayElementAtIndex(0).stringValue = "안녕!";
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(npc, NpcPrefabPath);
        Object.DestroyImmediate(npc);

        GameObject old = GameObject.Find("ChibiNPC");
        while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find("ChibiNPC"); }
        SpawnPoint hut = null;
        foreach (SpawnPoint sp in Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)) if (sp.Id == "FromHut") hut = sp;
        if (hut == null) return "집 앞 도착 지점(FromHut)을 찾지 못했어요.";
        Terrain terrain = Terrain.activeTerrain;
        Vector3 pos = hut.transform.position + new Vector3(-2.8f, 0f, -4f);
        pos.y = terrain != null ? terrain.SampleHeight(pos) + terrain.transform.position.y : hut.transform.position.y;
        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath));
        placed.name = "ChibiNPC";
        placed.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(hut.transform.position - pos));

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "치비를 만들었어요: NPC 위치 " + pos + ", 꾸밈 모델 " + DecoPrefabPath;
    }
}
