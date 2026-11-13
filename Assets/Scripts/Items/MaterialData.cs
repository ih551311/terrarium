using UnityEngine;

/// <summary>재료의 종류 (기획서 5장의 "종류" 칸)</summary>
public enum MaterialKind
{
    Plant,      // 식물 (자란다)
    Decoration, // 장식
    Ground,     // 바닥재
    Cover       // 바닥 덮기
}

/// <summary>모델이 없을 때 대신 쓰는 기본 도형</summary>
public enum PlaceholderShape
{
    Sphere,
    Capsule,
    Cube
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

    [Header("병 안에 놓았을 때 모습")]
    [Tooltip("진짜 모델(프리팹). 비워 두면 아래 기본 도형을 쓴다. 바닥이 오브젝트의 맨 아래에 오게 만든다.")]
    public GameObject placedPrefab;

    [Tooltip("프리팹이 없을 때 쓰는 기본 도형")]
    public PlaceholderShape placeholderShape = PlaceholderShape.Sphere;

    public Color placeholderColor = Color.gray;

    [Tooltip("기본 도형의 크기(미터). 병 안이 작아서 0.05~0.1 정도가 알맞다.")]
    public Vector3 placeholderSize = new Vector3(0.07f, 0.07f, 0.07f);
}
