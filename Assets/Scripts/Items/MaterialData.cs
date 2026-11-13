using UnityEngine;

/// <summary>재료의 종류 (기획서 5장의 "종류" 칸)</summary>
public enum MaterialKind
{
    Plant,      // 식물 (자란다)
    Decoration, // 장식
    Ground,     // 바닥재
    Cover       // 바닥 덮기
}

/// <summary>
/// 재료 하나의 정보. 코드에 직접 적지 않고 에셋으로 만든다.
/// Project 창에서 우클릭 > Create > 유리병 정원 > 재료 로 새 재료를 추가한다.
/// </summary>
[CreateAssetMenu(fileName = "NewMaterial", menuName = "유리병 정원/재료")]
public class MaterialData : ScriptableObject
{
    [Tooltip("저장할 때 쓰는 고유 이름 (영문, 바꾸지 말 것). 예: pinecone")]
    public string id;

    [Tooltip("화면에 보일 이름. 예: 솔방울")]
    public string displayName;

    public MaterialKind kind;

    [Tooltip("도감과 재료 칸에 쓸 그림 (나중에 연결)")]
    public Sprite icon;
}
