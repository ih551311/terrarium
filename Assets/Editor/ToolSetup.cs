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
            settings.axeLevels = DefaultLevels();
            settings.pickaxeLevels = DefaultLevels();
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

    private static ToolSettings.Level[] DefaultLevels()
    {
        return new[]
        {
            new ToolSettings.Level { hits = 4, amount = 1, rareChance = 0.03f, upgradeCost = 0 },
            new ToolSettings.Level { hits = 3, amount = 1, rareChance = 0.06f, upgradeCost = 30 },
            new ToolSettings.Level { hits = 2, amount = 2, rareChance = 0.10f, upgradeCost = 80 },
            new ToolSettings.Level { hits = 2, amount = 3, rareChance = 0.15f, upgradeCost = 150 }
        };
    }
}
