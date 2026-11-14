using UnityEngine;

/// <summary>
/// 발소리 소리 모음. 사신 에셋 "Footsteps Sounds - Volume 02"(The Sound Guild)의 소리를 바닥 종류별로 담는다.
/// 메뉴 "유리병 정원 > 발소리 연결"이 Assets/Resources/FootstepSet.asset 으로 만들어 준다. FootstepPlayer가 읽는다.
/// </summary>
public class FootstepSet : ScriptableObject
{
    [Tooltip("풀밭 (섬의 대부분)")] public AudioClip[] grass = new AudioClip[0];
    [Tooltip("바닷가 모래")] public AudioClip[] sand = new AudioClip[0];
    [Tooltip("마른 흙, 단단하고 푹신한 바닥")] public AudioClip[] soil = new AudioClip[0];
    [Tooltip("집 안 나무 바닥")] public AudioClip[] wood = new AudioClip[0];
    [Tooltip("비 오는 날의 젖은 땅 (철벅)")] public AudioClip[] wet = new AudioClip[0];
}
