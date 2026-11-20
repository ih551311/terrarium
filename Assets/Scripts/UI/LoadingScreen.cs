using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 집을 드나들거나 배를 타고 섬을 옮길 때 뜨는 로딩 화면. 화면은 Assets/Resources/UI/LoadingScreen.prefab (캔버스)에 있다.
/// 섬에서 얻는 재료 하나를 골라 그림과 함께 "어디서, 언제, 어떻게 얻는지"를 알려 준다(우체통에서 사는 물건은 빼고). 날씨나 밤 같은 조건이 있는 재료와 희귀 재료가 더 자주 나온다.
/// 쪽지 내용은 재료 데이터(구역, 얻는 방법, 나오는 때)에서 만들고, 낚시나 눈사람처럼 따로 얻는 재료는 아래 Special 표에 적었다.
/// GameRoot.GoTo가 부른다: 화면을 덮고(Show) → 씬을 불러오고 → 최소 MinSeconds 동안 보여 준 뒤 걷힌다(Hide).
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    /// <summary>쪽지를 읽을 시간만큼은 보여 준다(초, 실제 시간).</summary>
    public const float MinSeconds = 2.4f;
    private const float FadeSeconds = 0.25f;

    private static LoadingScreen instance;

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text destination, title, tip;
    [SerializeField] private Image icon;
    private string destinationText;

    // 재료 데이터만으로는 알 수 없는 재료의 쪽지
    private static readonly Dictionary<string, string> Special = new Dictionary<string, string>
    {
        { "firefly",       "밤에 섬 곳곳을 날아다녀요. 가까이 가서 E로 잡아요. 비나 눈이 오는 밤에는 안 나와요." },
        { "smelt",         "눈 섬의 얼어붙은 연못에서 낚시로 잡아요. 얼음 구멍 앞에서 E." },
        { "ice_trout",     "눈 섬의 얼어붙은 연못에서 낚시로 잡아요. 찌가 흔들리면 바로 E." },
        { "goldfish",      "눈 섬 얼음 낚시에서 가끔 걸려요." },
        { "aurora_fish",   "오로라가 뜨는 밤, 눈 섬 얼음 낚시에서 아주 가끔 걸려요." },
        { "mini_snowman",  "눈 섬 마을의 눈사람을 곡괭이로 캐면 나와요. 다음 날 아침이면 눈사람이 다시 서 있어요." },
        { "cactus_piece",  "사막 섬의 선인장을 도끼로 치면 나와요." },
        { "date_fruit",    "사막 섬 오아시스의 야자나무를 도끼로 치면 떨어져요." },
        { "red_sand",      "사막 섬의 붉은 모래 지대에서 주워요. 병 바닥에 깔 수 있어요." },
        { "sand",          "바닷가에서 주워요. 병 바닥에 깔 수 있어요." },
        { "snow",          "눈 섬 어디서나 주울 수 있어요. 병 바닥에 깔 수 있어요." },
        { "moss_ground",   "우체통에서 살 수 있어요. 병 바닥에 폭신하게 깔 수 있어요." },
        { "pottery_shard", "사막 섬 피라미드 둘레에 흩어져 있어요." },
        { "scarab",        "사막 섬에서 야영지와 멀리 떨어진 모래밭에 숨어 있어요." },
        { "fossil",        "사막 섬 바위를 곡괭이로 칠 때 아주 가끔 나와요." },
        { "snow_flower",   "눈 오는 날 꽃 언덕에 피어요. 늘 눈이 오는 눈 섬에서도 찾을 수 있어요." },
        { "ice_shard",     "눈 섬의 눈 더미를 곡괭이로 캐면 나와요." },
        { "icicle",        "눈 섬의 눈 더미를 곡괭이로 캐면 나와요." },
        { "desert_rose",   "사막 섬 바위를 곡괭이로 치면 나와요." },
        { "sandstone",     "사막 섬 바위를 곡괭이로 치면 나와요." },
        { "pine_sprig",    "눈 섬의 소나무를 도끼로 치면 나와요." },
        { "snowy_pinecone","눈 섬의 소나무를 도끼로 치면 나와요." },
        { "firewood",      "눈 섬의 소나무를 도끼로 치면 나와요." },
        { "amber",         "나무를 도끼로 칠 때 아주 가끔 나와요. 도끼를 강화하면 더 자주." },
        { "crystal",       "돌을 곡괭이로 칠 때 아주 가끔 나와요. 곡괭이를 강화하면 더 자주." },
    };

    private static string ZoneName(MaterialZone zone)
    {
        switch (zone)
        {
            case MaterialZone.Forest: return "숲";
            case MaterialZone.Beach: return "바닷가";
            case MaterialZone.FlowerHill: return "꽃 언덕";
            case MaterialZone.Snow: return "눈 섬";
            case MaterialZone.Desert: return "사막 섬";
            default: return "섬 곳곳";
        }
    }

    private static string WhenText(MaterialCondition c)
    {
        switch (c)
        {
            case MaterialCondition.NightOnly: return "밤에만 ";
            case MaterialCondition.AfterRain: return "비 오는 날이나 그다음 날, ";
            case MaterialCondition.SnowDay: return "눈 오는 날에만 ";
            case MaterialCondition.FogDay: return "안개 낀 날에만 ";
            case MaterialCondition.AfterStorm: return "폭풍우 치는 날이나 그다음 날, ";
            case MaterialCondition.WindDay: return "바람 부는 날에만 ";
            case MaterialCondition.StormOnly: return "폭풍우 치는 날에만 ";
            case MaterialCondition.RainbowDay: return "비가 그치고 갠 날에만 ";
            default: return string.Empty;
        }
    }

    /// <summary>재료 하나의 쪽지: 어디서, 언제, 어떻게 얻는지</summary>
    public static string TipOf(MaterialData m)
    {
        if (Special.TryGetValue(m.id, out string special)) return special;
        if (m.zone == MaterialZone.Shop || m.acquire == AcquireMethod.Shop)
            return "우체통 구매하기에서 " + m.buyPrice + "코인에 살 수 있어요. 다음 날 아침에 도착해요.";

        string where = ZoneName(m.zone), when = WhenText(m.condition);
        switch (m.acquire)
        {
            case AcquireMethod.Axe:
                return where + "의 나무를 도끼로 치면 " + (m.isRare ? "아주 가끔 " : "") + "나와요.";
            case AcquireMethod.Pickaxe:
                return where + "의 돌을 곡괭이로 치면 " + (m.isRare ? "아주 가끔 " : "") + "나와요.";
            case AcquireMethod.Hoe:
                return (m.isRare ? "아주 드물게, " : "") + (m.plantGroup == PlantGroup.Flower ? "꽃" : "풀") + "을 괭이로 캐면 나와요." + (m.zone != MaterialZone.Anywhere ? " " + where + "에서 잘 나와요." : "");
            default:
                bool plant = m.kind == MaterialKind.Plant || m.kind == MaterialKind.Cover;
                return when + where + "에서 " + (plant ? "캘 수 있어요. 바라보고 E." : "주울 수 있어요. 바라보고 E.");
        }
    }

    /// <summary>쪽지에 쓸 재료를 고른다. 조건이 있는 것, 희귀한 것, 따로 얻는 것을 더 자주 고른다.</summary>
    private static MaterialData PickMaterial()
    {
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db == null || db.materials == null || db.materials.Length == 0) return null;
        var weighted = new List<MaterialData>();
        foreach (MaterialData m in db.materials)
        {
            if (m == null || m.icon == null) continue;
            int w = 1;
            if (m.condition != MaterialCondition.Always) w += 3;
            if (m.isRare || Special.ContainsKey(m.id)) w += 2;
            if (m.zone == MaterialZone.Shop || m.acquire == AcquireMethod.Shop) continue; // 우체통에서 사는 물건은 얻는 조건이 없어서 쪽지에 넣지 않는다
            for (int i = 0; i < w; i++) weighted.Add(m);
        }
        return weighted.Count > 0 ? weighted[Random.Range(0, weighted.Count)] : null;
    }

    private static string DestinationOf(string sceneName)
    {
        switch (sceneName)
        {
            case "SnowIsland": return "배를 타고 눈 섬으로 가는 중";
            case "DesertIsland": return "배를 타고 사막 섬으로 가는 중";
            default: return "섬으로 나가는 중";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private static LoadingScreen Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject prefab = Resources.Load<GameObject>("UI/LoadingScreen");
                if (prefab == null) throw new System.InvalidOperationException("UI/LoadingScreen 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요.");
                GameObject go = Instantiate(prefab);
                go.name = "LoadingScreen";
                DontDestroyOnLoad(go);
                instance = go.GetComponent<LoadingScreen>();
                instance.group.alpha = 0f;
                instance.group.blocksRaycasts = false;
            }
            return instance;
        }
    }
    /// <summary>화면을 덮는다 (sceneName은 갈 곳). 다 덮일 때까지 기다린다.</summary>
    public static IEnumerator Show(string sceneName)
    {
        LoadingScreen s = Instance;
        MaterialData m = PickMaterial();
        s.title.text = m != null ? m.displayName : string.Empty;
        s.tip.text = m != null ? TipOf(m) : string.Empty;
        s.icon.sprite = m != null ? m.icon : null;
        s.icon.enabled = m != null && m.icon != null;
        s.destinationText = DestinationOf(sceneName);
        s.destination.text = s.destinationText;
        s.group.blocksRaycasts = true;
        yield return s.Fade(1f);
    }

    /// <summary>화면을 걷는다.</summary>
    public static IEnumerator Hide()
    {
        LoadingScreen s = Instance;
        yield return s.Fade(0f);
        s.group.blocksRaycasts = false;
    }

    private IEnumerator Fade(float target)
    {
        float start = group.alpha, t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, t / FadeSeconds);
            yield return null;
        }
        group.alpha = target;
    }

    private void Update()
    {
        // 가는 중 점 세 개가 하나씩 늘어난다
        if (group == null || group.alpha <= 0f || destination == null) return;
        int dots = 1 + (int)(Time.unscaledTime * 2.5f) % 3;
        destination.text = destinationText + new string('.', dots);
    }
}
