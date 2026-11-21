using UnityEngine;

/// <summary>
/// 도구로 칠 때 나는 "툭" 소리. 소리 파일이 아직 없어서 코드로 짧은 소리를 만들어 쓴다.
/// 나중에 진짜 소리 파일을 구하면 이 스크립트만 바꾸면 된다.
/// </summary>
public static class ToolSound
{
    private static AudioClip clip;
    private static AudioClip[] files; // Resources/Sfx/ToolHit1~3 (진짜 녹음). 없으면 코드로 만든 소리

    public static void Play(Vector3 position, float pitchScale = 1f)
    {
        if (files == null)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 3; i++) { AudioClip f = Resources.Load<AudioClip>("Sfx/ToolHit" + i); if (f != null) list.Add(f); }
            files = list.ToArray();
        }
        if (files.Length > 0)
        {
            AudioSource.PlayClipAtPoint(files[Random.Range(0, files.Length)], position, 0.7f * GameAudio.SfxVolume);
            return;
        }
        if (clip == null) clip = MakeClip();
        AudioSource.PlayClipAtPoint(clip, position, 0.7f * GameAudio.SfxVolume);
    }

    private static AudioClip MakeClip()
    {
        const int Rate = 22050;
        int samples = (int)(Rate * 0.16f);
        float[] data = new float[samples];
        var random = new System.Random(7);
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)Rate;
            float decay = Mathf.Exp(-t * 28f);
            float thump = Mathf.Sin(2f * Mathf.PI * 150f * t);
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            data[i] = (thump * 0.7f + noise * 0.3f) * decay;
        }
        AudioClip c = AudioClip.Create("ToolHit", samples, 1, Rate, false);
        c.SetData(data, 0);
        return c;
    }

    // Play를 껐다 켤 때 만든 소리를 버린다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        clip = null;
        files = null;
    }
}
