using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 사신 에셋 "Footsteps Sounds - Volume 02"(Assets/The_Sound_Guild_FREE_PACK_Footsteps_Volume_02)의 소리를
/// 바닥 종류별로 모아 Assets/Resources/FootstepSet.asset 을 만든다. 바닥 종류와 고른 소리는 아래 표에서 고친다.
///   풀밭   Foostep_Grass(철자가 에셋 원본 그대로), Footstep_Grass_Alternative
///   모래   Footstep_Sand
///   흙     Footstep_Soft_Surface
///   나무   Footstep_Wood
///   젖은 땅 Footstep_Mud (비 오는 날에 섞인다)
/// 메뉴: 유리병 정원 > 발소리 연결
/// </summary>
public static class FootstepSetup
{
    private const string Folder = "Assets/The_Sound_Guild_FREE_PACK_Footsteps_Volume_02";
    private const string AssetPath = "Assets/Resources/FootstepSet.asset";
    private const string SnowFolder = "Assets/Audio/SnowSteps";

    [MenuItem("유리병 정원/발소리 연결")]
    public static string Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        FootstepSet set = AssetDatabase.LoadAssetAtPath<FootstepSet>(AssetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<FootstepSet>();
            AssetDatabase.CreateAsset(set, AssetPath);
        }

        set.grass = Find("Foostep_Grass_", "Footstep_Grass_Alternative_");
        set.sand = Find("Footstep_Sand_");
        set.soil = Find("Footstep_Soft_Surface_");
        set.wood = Find("Footstep_Wood_");
        set.wet = Find("Footstep_Mud_");
        // 눈 섬의 눈 소리: 사신 팩의 모래 발소리를 느리고 둔하게 가공한 것(Assets/Audio/SnowSteps). 없으면 FootstepPlayer가 흙 소리를 쓴다
        set.snow = FindIn(SnowFolder, "Snow_");
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        return "발소리 연결: 눈 " + set.snow.Length + ", 풀밭 " + set.grass.Length + ", 모래 " + set.sand.Length + ", 흙 " + set.soil.Length + ", 나무 " + set.wood.Length + ", 젖은 땅 " + set.wet.Length;
    }

    /// <summary>파일 이름이 prefixes 중 하나로 시작하는 소리를 모은다</summary>
    private static AudioClip[] Find(params string[] prefixes)
    {
        return FindIn(Folder, prefixes);
    }

    private static AudioClip[] FindIn(string folder, params string[] prefixes)
    {
        var list = new List<AudioClip>();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);
            foreach (string p in prefixes)
                if (file.StartsWith(p)) { list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path)); break; }
        }
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list.ToArray();
    }
}
