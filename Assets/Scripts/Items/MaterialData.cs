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

/// <summary>재료가 나는 곳 (기획서 4장의 구역)</summary>
public enum MaterialZone
{
    Anywhere,   // 어느 구역에서나 (희귀 재료)
    Forest,     // 작은 숲
    Beach,      // 바닷가
    FlowerHill, // 꽃 언덕
    Shop,       // 자연에서는 못 구하고 우체통 상점에서만 산다
    Snow,       // 눈 섬(SnowIsland 씬)에서만 난다
    Desert      // 사막 섬(DesertIsland 씬)에서만 난다
}

/// <summary>재료를 얻는 방법 (기획서 5장, 6.8장)</summary>
public enum AcquireMethod
{
    Hand,     // 맨손으로 줍는다 (E 키)
    Axe,      // 도끼로 나무를 친다
    Pickaxe,  // 곡괭이로 돌을 친다
    Shop,     // 우체통에서 코인으로 주문한다
    Hoe       // 섬에 자란 풀과 꽃을 괭이로 캐면 무작위로 나온다 (따로 놓인 자리는 없다)
}

/// <summary>괭이로 캘 수 있는 식물의 갈래. 풀처럼 생긴 것에서는 풀 재료가, 꽃처럼 생긴 것에서는 꽃 재료가 나온다.</summary>
public enum PlantGroup
{
    None,   // 괭이로 캐지 않는 재료
    Grass,  // 풀, 이끼, 잎, 열매, 버섯 같은 풀숲의 것들
    Flower  // 꽃
}

/// <summary>재료가 나오는 때 (낮과 밤·날씨는 9~10단계에서 켠다. 그전에는 항상 나온다)</summary>
public enum MaterialCondition
{
    Always,
    NightOnly,
    AfterRain,
    SnowDay,    // 눈 오는 날에만
    FogDay,     // 안개 낀 날에만
    AfterStorm, // 폭풍우가 지나간 다음 날(과 폭풍우 치는 날)
    WindDay,    // 바람 부는 날에만
    StormOnly,  // 폭풍우 치는 날에만 (번개가 친 날)
    RainbowDay  // 비가 그친 뒤 맑게 갠 날에만 (무지개가 뜨는 날)
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

    [Header("어디서 어떻게 구하는가")]
    [Tooltip("재료가 나는 구역. Shop이면 자연에는 없고 상점에서만 산다.")]
    public MaterialZone zone = MaterialZone.Anywhere;

    [Tooltip("재료를 얻는 방법. 도끼와 곡괭이는 11단계에서 만든다. 그전에는 섬 바닥에 임시로 놓인 것을 줍는다.")]
    public AcquireMethod acquire = AcquireMethod.Hand;

    [Tooltip("나오는 때. 낮과 밤·날씨는 9~10단계에서 켠다.")]
    public MaterialCondition condition = MaterialCondition.Always;

    [Tooltip("희귀 재료인가 (도끼나 곡괭이로 칠 때 가끔 나온다)")]
    public bool isRare;

    [Tooltip("괭이로 캘 때 풀에서 나오는가, 꽃에서 나오는가. 괭이로 얻지 않는 재료는 None")]
    public PlantGroup plantGroup;

    [Header("코인 (12~13단계의 우체통 상점에서 쓴다. 모두 임시 값)")]
    [Tooltip("우체통에 넣어 팔 때 받는 코인 (기본 재료 1~3, 희귀 재료 20)")]
    public int sellPrice = 1;

    [Tooltip("우체통에서 살 때 내는 코인. 0이면 상점에서 팔지 않는다.")]
    public int buyPrice;

    [Header("병 안에 놓았을 때 모습")]
    [Tooltip("진짜 모델(프리팹). 비워 두면 아래 기본 도형을 쓴다. 바닥이 오브젝트의 맨 아래에 오게 만든다.")]
    public GameObject placedPrefab;

    [Tooltip("프리팹이 없을 때 쓰는 기본 도형")]
    public PlaceholderShape placeholderShape = PlaceholderShape.Sphere;

    public Color placeholderColor = Color.gray;

    [Tooltip("기본 도형의 크기(미터). 병 안이 작아서 0.05~0.1 정도가 알맞다.")]
    public Vector3 placeholderSize = new Vector3(0.07f, 0.07f, 0.07f);

    [Header("섬 바닥에 놓였을 때 모습")]
    [Tooltip("섬에 놓을 때 모델을 몇 배로 키울지. 모델이 4~12cm로 작아서 병 밖에서는 크게 보여야 주울 수 있다.")]
    public float pickupScale = 4f;
}
