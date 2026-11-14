using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 도끼와 곡괭이의 설정 에셋(Assets/Resources/ToolSettings.asset)을 만든다. (기획서 6.8의 임시 값)
/// 이미 있으면 재료 목록만 다시 채우고, 수치는 손으로 고친 것을 지키지 않고 기본값으로 되돌리지 않는다.
/// 메뉴: 유리병 정원 > 도구 설정 만들기.
/// </summary>
public static class ToolSetup
{
    private const string AssetPath = "Assets/Resources/ToolSettings.asset";
    private const string MaterialFolder = "Assets/Data/Materials";

    [MenuItem("유리병 정원/도구 설정 만들기")]
    public static string Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        ToolSettings settings = AssetDatabase.LoadAssetAtPath<ToolSettings>(AssetPath);
        bool created = settings == null;
        if (created)
        {
            settings = ScriptableObject.CreateInstance<ToolSettings>();
            // 기획서 6.8의 임시 값: 타격 수, 얻는 양, 희귀 확률, 강화 비용
            ApplyDefaultLevels(settings);
            AssetDatabase.CreateAsset(settings, AssetPath);
        }

        // 도끼나 곡괭이로 얻는 재료(희귀 포함)를 모두 모은다
        var list = new List<MaterialData>();
        foreach (string guid in AssetDatabase.FindAssets("t:MaterialData", new[] { MaterialFolder }))
        {
            MaterialData m = AssetDatabase.LoadAssetAtPath<MaterialData>(AssetDatabase.GUIDToAssetPath(guid));
            if (m != null && (m.acquire == AcquireMethod.Axe || m.acquire == AcquireMethod.Pickaxe)) list.Add(m);
        }
        settings.harvestMaterials = list.ToArray();

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return (created ? "도구 설정을 만들었어요" : "도구 설정의 재료 목록을 갱신했어요") + ": 캘 수 있는 재료 " + list.Count + "종";
    }

    /// <summary>
    /// 캔 나무의 잎을 숨길 때 잎 칸에 끼우는 투명 재질(Assets/Resources/HiddenLeaf.mat)을 만든다.
    /// 모든 픽셀을 버리는(Alpha Clip) 재질이라 그림자도 생기지 않는다.
    /// </summary>
    public static Material EnsureHiddenLeafMaterial()
    {
        const string path = "Assets/Resources/HiddenLeaf.mat";
        Directory.CreateDirectory("Assets/Resources");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
        return m;
    }

    /// <summary>
    /// 도끼, 곡괭이, 괭이의 단계별 수치를 기본값으로 되돌린다. 손으로 고친 값도 덮어쓴다.
    /// 처음 장비(0단계)는 여러 번 쳐야 하고, 강화할수록 적게 치고 많이 얻고 희귀 재료가 자주 나온다.
    /// 메뉴: 유리병 정원 > 도구 수치 기본값으로.
    /// </summary>
    [MenuItem("유리병 정원/도구 수치 기본값으로")]
    public static string ResetLevels()
    {
        ToolSettings settings = AssetDatabase.LoadAssetAtPath<ToolSettings>(AssetPath);
        if (settings == null) return Build();
        ApplyDefaultLevels(settings);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return "도구 수치를 기본값으로 되돌렸어요";
    }

    private static void ApplyDefaultLevels(ToolSettings settings)
    {
        //                          치는 횟수(0~3단계)    얻는 양            희귀 확률                     강화 비용
        settings.axeLevels     = Levels(new[] { 8, 6, 4, 3 }, new[] { 1, 1, 2, 3 }, new[] { 0.03f, 0.06f, 0.10f, 0.15f }, new[] { 0, 30, 80, 150 });
        settings.pickaxeLevels = Levels(new[] { 8, 6, 4, 3 }, new[] { 1, 1, 2, 3 }, new[] { 0.03f, 0.06f, 0.10f, 0.15f }, new[] { 0, 30, 80, 150 });
        settings.hoeLevels     = Levels(new[] { 6, 4, 3, 2 }, new[] { 1, 1, 2, 3 }, new[] { 0.03f, 0.06f, 0.10f, 0.15f }, new[] { 0, 20, 60, 120 });
    }

    private static ToolSettings.Level[] Levels(int[] hits, int[] amounts, float[] rare, int[] costs)
    {
        var levels = new ToolSettings.Level[hits.Length];
        for (int i = 0; i < levels.Length; i++)
            levels[i] = new ToolSettings.Level { hits = hits[i], amount = amounts[i], rareChance = rare[i], upgradeCost = costs[i] };
        return levels;
    }
}
