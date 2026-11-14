using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 눈 에셋(Assets/HobiSoloved)의 건물(오두막 3종, 우물)을 병 속에 놓는 꾸밈 재료의 모델로 만든다.
/// 원래 모델은 1.8m 크기라, 눈 에셋 프리팹을 아주 작게(0.05배, 9cm 안팎) 줄여 담은 새 프리팹을
/// Assets/Models/Deco/deco_snow_*.prefab 으로 저장한다. (재질은 눈 에셋의 것을 그대로 쓴다)
/// 재료 데이터는 MaterialSetup의 표(눈 오두막 1~3, 눈 우물)가 만들고, 우체통에서 코인으로 산다.
/// 메뉴: 유리병 정원 > 눈 건물 재료 모델 만들기 (그다음 재료 데이터 만들기, 재료 목록 만들기, 재료 아이콘 만들기, UI 만들기)
/// </summary>
public static class BuildingMaterialSetup
{
    private const float Scale = 0.05f;
    private const string ModelFolder = "Assets/Models/Deco";

    private static readonly (string source, string output)[] Buildings =
    {
        ("Cottage1", "deco_snow_cottage1"),
        ("Cottage2", "deco_snow_cottage2"),
        ("Cottage3", "deco_snow_cottage3"),
        ("Well", "deco_snow_well"),
    };

    [MenuItem("유리병 정원/눈 건물 재료 모델 만들기")]
    public static string Build()
    {
        SnowAssetSetup.ConvertMaterials();
        int made = 0;
        foreach ((string source, string output) in Buildings)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(SnowAssetSetup.Folder + "/Prefabs/" + source + ".prefab");
            if (src == null) { Debug.LogWarning("눈 에셋 프리팹을 찾지 못했어요: " + source); continue; }

            GameObject root = new GameObject(output);
            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(src);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = src.transform.localScale * Scale;

            string path = ModelFolder + "/" + output + ".prefab";
            Directory.CreateDirectory(ModelFolder);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            made++;
        }
        AssetDatabase.SaveAssets();
        return "눈 건물 재료 모델 " + made + "개를 만들었어요 (" + ModelFolder + "/deco_snow_*.prefab)";
    }
}
