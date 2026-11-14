using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경 음악 재생기. 때(낮, 밤)와 날씨에 어울리는 곡을 골라 틀고, 바뀌면 천천히 섞어 넘어간다.
/// GameAudio가 만든다. 곡은 MusicSet(Resources/MusicSet)에 있고, 없으면 쓸 수 없다(GameAudio가 코드로 만든 음악을 쓴다).
///
/// 곡 고르기 (위에서부터 먼저 맞는 것):
///   눈     → Winter
///   폭풍우 → Purgatory, Oddity            (낮고 어두운 울림)
///   비     → Fragments, Tone, Tone 2       (방울 같은 소리와 고른 울림)
///   안개, 흐림 → Oddity, Tone, Tone 2
///   밤     → Lost, Tone 2, Fragments       (성기고 조용한 곡)
///   그 밖의 낮 → Ice Cream, Tone 2, Tone   (밝고 느긋한 곡)
/// 같은 상황이 이어지면 한 곡이 약 2분 반 틀어진 뒤 같은 모음의 다른 곡으로 넘어가서 질리지 않게 한다.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    private const float CrossFadeSeconds = 6f;
    private const float RotateSeconds = 150f;

    private static readonly Dictionary<string, string[]> Playlists = new Dictionary<string, string[]>
    {
        { "snow",  new[] { "winter" } },
        { "storm", new[] { "purgatory", "oddity" } },
        { "rain",  new[] { "fragments", "tone", "tone2" } },
        { "gray",  new[] { "oddity", "tone", "tone2" } },
        { "night", new[] { "lost", "tone2", "fragments" } },
        { "day",   new[] { "icecream", "tone2", "tone" } }
    };

    private MusicSet set;
    private AudioSource[] sources = new AudioSource[2];
    private float[] fades = new float[2];    // 각 재생 장치가 얼마나 들리는가 0~1
    private float[] gains = new float[2];
    private int active = -1;                 // 지금 틀고 있는 장치 (-1 아직 없음)
    private string currentId, currentContext;
    private float startedAt;

    /// <summary>쓸 수 있는 곡이 있는가</summary>
    public bool Ready => set != null && set.tracks.Length > 0;

    public bool Load()
    {
        set = Resources.Load<MusicSet>("MusicSet");
        if (!Ready) return false;
        for (int i = 0; i < 2; i++)
        {
            GameObject go = new GameObject("Music" + i);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            sources[i] = s;
        }
        return true;
    }

    /// <summary>매 프레임 부른다. level은 전체 음악 크기(0이면 조용히)이고 night는 밤의 정도(0 낮~1 밤)다.</summary>
    public void Tick(float level, float night, WeatherKind weather)
    {
        if (!Ready) return;

        string context = ContextOf(night, weather);
        bool rotate = active >= 0 && Time.time - startedAt > RotateSeconds && Playlists[context].Length > 1;
        if (active < 0 || context != currentContext || rotate) Next(context);

        // 새 곡은 커지고 이전 곡은 작아진다
        for (int i = 0; i < 2; i++)
        {
            float target = i == active ? 1f : 0f;
            fades[i] = Mathf.MoveTowards(fades[i], target, Time.deltaTime / CrossFadeSeconds);
            sources[i].volume = level * fades[i] * gains[i];
            if (fades[i] <= 0f && sources[i].isPlaying && i != active) sources[i].Stop();
        }
    }

    private static string ContextOf(float night, WeatherKind weather)
    {
        if (weather == WeatherKind.Snow) return "snow";
        if (weather == WeatherKind.Storm) return "storm";
        if (night > 0.5f) return "night";
        if (weather == WeatherKind.Rain || weather == WeatherKind.Drizzle) return "rain";
        if (weather == WeatherKind.Fog || weather == WeatherKind.Cloudy) return "gray";
        return "day";
    }

    /// <summary>이 분위기의 곡 중에서 지금 곡이 아닌 것을 무작위로 골라 틀기 시작한다.</summary>
    private void Next(string context)
    {
        string[] list = Playlists[context];
        var candidates = new List<string>();
        foreach (string id in list) if (id != currentId && set.Find(id) != null) candidates.Add(id);
        if (candidates.Count == 0) foreach (string id in list) if (set.Find(id) != null) candidates.Add(id);
        if (candidates.Count == 0) return;

        string pick = candidates[Random.Range(0, candidates.Count)];
        MusicSet.Track track = set.Find(pick);

        int next = active == 0 ? 1 : 0;
        AudioSource s = sources[next];
        s.Stop();
        s.clip = track.clip;
        s.time = 0f;
        s.volume = 0f;
        s.Play();
        gains[next] = track.gain;
        fades[next] = 0f;

        active = next;
        currentId = pick;
        currentContext = context;
        startedAt = Time.time;
    }
}
