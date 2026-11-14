using System;
using UnityEngine;

/// <summary>
/// 배경 음악 곡 모음. 사신 에셋 "Dreamcore Music Vol. 1"의 이음매 없이 반복되는(LOOP) 곡 8개를 담는다.
/// 메뉴 "유리병 정원 > 배경 음악 연결"이 Assets/Resources/MusicSet.asset 으로 만들어 준다. MusicPlayer가 읽는다.
/// </summary>
public class MusicSet : ScriptableObject
{
    [Serializable]
    public class Track
    {
        [Tooltip("곡 이름표 (코드가 분위기에 맞는 곡을 고를 때 쓴다): tone, icecream, lost, oddity, tone2, purgatory, winter, fragments")]
        public string id;
        public AudioClip clip;
        [Tooltip("곡마다 소리 크기가 달라서 맞추는 값 (1이 기본). 시끄러운 곡은 작게")]
        [Range(0.3f, 1.5f)] public float gain = 1f;
    }

    public Track[] tracks = new Track[0];

    public Track Find(string id)
    {
        foreach (Track t in tracks) if (t != null && t.id == id && t.clip != null) return t;
        return null;
    }
}
