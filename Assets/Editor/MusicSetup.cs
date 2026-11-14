using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 사신 에셋 "Dreamcore Music Vol. 1"(Assets/Dreamcore Music)의 이음매 없는 반복(LOOP) 곡 8개를 게임 음악으로 연결한다.
///  1) 곡의 가져오기 설정을 "스트리밍"으로 바꾼다. (곡이 길어서 통째로 메모리에 올리면 무겁다)
///  2) Assets/Resources/MusicSet.asset 을 만들어 곡마다 이름표와 소리 크기 맞춤값을 넣는다.
/// 곡 고르기는 MusicPlayer가 한다. 메뉴: 유리병 정원 > 배경 음악 연결
/// </summary>
public static class MusicSetup
{
    private const string Folder = "Assets/Dreamcore Music";
    private const string AssetPath = "Assets/Resources/MusicSet.asset";

    // 이름표, 파일 이름에 들어 있는 글자, 소리 크기 맞춤(곡 음량을 재서 시끄러운 곡은 작게)
    private static readonly (string id, string file, float gain)[] Tracks =
    {
        ("tone",      "LOOP Dreamcore Tone (by",   0.65f), // 고르고 큰 곡
        ("icecream",  "LOOP Dreamcore Ice Cream",  1.0f),
        ("lost",      "LOOP Dreamcore Lost",       1.15f), // 성기고 조용한 곡
        ("oddity",    "LOOP Dreamcore Oddity",     1.15f),
        ("tone2",     "LOOP Dreamcore Tone 2",     0.85f),
        ("purgatory", "LOOP Dreamcore Purgatory",  0.85f),
        ("winter",    "LOOP Dreamcore Winter",     1.0f),
        ("fragments", "LOOP Dreamcore Fragments",  1.0f),
    };

    [MenuItem("유리병 정원/배경 음악 연결")]
    public static string Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        MusicSet set = AssetDatabase.LoadAssetAtPath<MusicSet>(AssetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<MusicSet>();
            AssetDatabase.CreateAsset(set, AssetPath);
        }

        var list = new System.Collections.Generic.List<MusicSet.Track>();
        string missing = string.Empty;
        foreach (var (id, file, gain) in Tracks)
        {
            string path = FindClip(file);
            if (path == null) { missing += id + " "; continue; }

            // 스트리밍으로: 재생할 때 조금씩 읽는다 (메모리를 적게 쓴다)
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();

            list.Add(new MusicSet.Track { id = id, clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path), gain = gain });
        }
        set.tracks = list.ToArray();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        return "배경 음악 " + list.Count + "곡을 연결했어요" + (missing.Length > 0 ? " (못 찾은 곡: " + missing + ")" : string.Empty);
    }

    private static string FindClip(string fileContains)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path).Contains(fileContains)) return path;
        }
        return null;
    }
}
