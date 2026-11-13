using UnityEngine;

/// <summary>
/// 병 한 종류의 정보 (둥근 병, 긴 병, 넓은 병). 코드에 직접 적지 않고 에셋으로 만든다.
/// 새 병을 추가하려면: 병 프리팹을 만들고(Assets/Prefabs/Bottles 참고) 이 데이터를 하나 더 만든다.
/// 빈 병이 매일 모래사장에 놓이는 것(10단계)과 선반(4단계)에서 이 데이터로 병을 만든다.
/// </summary>
[CreateAssetMenu(fileName = "NewBottle", menuName = "유리병 정원/병")]
public class BottleData : ScriptableObject
{
    [Tooltip("저장할 때 쓰는 고유 이름 (영문, 바꾸지 말 것). 예: round")]
    public string id;

    [Tooltip("화면에 보일 이름. 예: 둥근 병")]
    public string displayName;

    [Tooltip("병 프리팹 (유리, 흙, 뚜껑, 병 카메라, 꾸미기 스크립트가 들어 있다)")]
    public GameObject prefab;
}
