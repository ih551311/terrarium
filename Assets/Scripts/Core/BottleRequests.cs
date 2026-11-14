using System;
using System.Collections.Generic;

/// <summary>병 주문 편지 하나. 누가, 어떤 병을 원하는지, 조건, 답례.</summary>
public class BottleRequest
{
    public string from;           // 보낸 사람
    public string body;           // 편지 내용
    public string condition;      // 조건 한 줄 (화면에 보여 준다)
    public string thanks;         // 병을 보낸 뒤 답장
    public int coins;             // 답례 코인
    public string rewardItemId;   // 답례 재료 (없으면 null)
    public Func<BottleRecord, bool> test;
}

/// <summary>
/// 병 주문 편지 목록. 이따금 우체통에 편지가 와서 "이런 느낌의 병을 보내 주세요" 하고 부탁한다.
/// 기한도 실패도 없다. 조건에 맞는 완성한 병을 보내면 답례로 코인(과 가끔 재료)이 온다. 보낸 병은 돌아오지 않는다.
/// </summary>
public static class BottleRequests
{
    private static int Count(BottleRecord r, Func<MaterialData, bool> match)
    {
        int n = 0;
        foreach (PlacedData p in r.items) if (p.material != null && match(p.material)) n++;
        return n;
    }

    private static bool Has(BottleRecord r, string id)
    {
        foreach (PlacedData p in r.items) if (p.material != null && p.material.id == id) return true;
        return false;
    }

    private static bool HasAny(BottleRecord r, params string[] ids)
    {
        foreach (string id in ids) if (Has(r, id)) return true;
        return false;
    }

    private static int CountIds(BottleRecord r, params string[] ids)
    {
        int n = 0;
        foreach (PlacedData p in r.items)
        {
            if (p.material == null) continue;
            foreach (string id in ids) if (p.material.id == id) { n++; break; }
        }
        return n;
    }

    private static readonly BottleRequest[] All =
    {
        new BottleRequest { from = "등대지기 할머니",
            body = "바닷바람이 그리운 날이에요. 파도 소리가 들리는 것 같은 병을 하나 만들어 줄래요?",
            condition = "바닷가 재료를 3개 이상 넣은 병", coins = 40, rewardItemId = "seashell",
            thanks = "고마워요. 병을 귀에 대 보니 정말 파도 소리가 나는 것 같네요.",
            test = r => Count(r, m => m.zone == MaterialZone.Beach) + (r.ground != null && r.ground.id == "sand" ? 1 : 0) >= 3 },

        new BottleRequest { from = "숲지기 아저씨",
            body = "요즘 숲 냄새가 너무 그리워요. 이끼 낀 조용한 숲을 병에 담아 줄 수 있나요?",
            condition = "숲 재료를 3개 이상 넣은 병", coins = 40, rewardItemId = "acorn",
            thanks = "숲이 병 안에 쏙 들어왔네요! 창가에 두고 아침마다 볼게요.",
            test = r => Count(r, m => m.zone == MaterialZone.Forest) >= 3 },

        new BottleRequest { from = "꽃집 아가씨",
            body = "곧 있을 친구의 생일에 꽃이 가득한 병을 선물하고 싶어요. 예쁜 꽃밭을 부탁해요.",
            condition = "꽃을 4송이 이상 넣은 병", coins = 55, rewardItemId = "tulip",
            thanks = "와, 꽃밭이 통째로 들어 있네요. 친구가 정말 좋아할 거예요!",
            test = r => Count(r, m => m.plantGroup == PlantGroup.Flower) >= 4 },

        new BottleRequest { from = "별을 모으는 아이",
            body = "밤하늘을 병에 넣어 두고 싶어요. 달이나 별, 촛불처럼 밤에 어울리는 것들을 넣어 줄래요?",
            condition = "초, 별 장식, 초승달, 달맞이꽃, 반딧불이 중 2개 이상", coins = 60, rewardItemId = "star_ornament",
            thanks = "불을 끄고 보니까 병이 작은 밤하늘 같아요. 고마워요!",
            test = r => CountIds(r, "candle", "star_ornament", "crescent_moon", "evening_primrose", "firefly") >= 2 },

        new BottleRequest { from = "수집가 아저씨",
            body = "저는 가득 찬 것을 좋아해요. 빈틈없이 꽉 채운 병을 보고 싶어요.",
            condition = "재료를 10개 이상 넣은 병", coins = 60, rewardItemId = "crystal",
            thanks = "이렇게 가득 채우다니, 정성이 느껴져요. 이건 답례예요.",
            test = r => r.items.Count >= 10 },

        new BottleRequest { from = "모래성 아이",
            body = "나는 모래가 좋아요! 모래를 깔고 조개를 놓은 병을 만들어 줄 수 있어요?",
            condition = "바닥을 모래로 하고 재료를 3개 이상 넣은 병", coins = 45, rewardItemId = "sand_dollar",
            thanks = "모래 위에 조개가 놓여 있어서 꼭 바닷가 같아요. 최고예요!",
            test = r => r.ground != null && r.ground.id == "sand" && r.items.Count >= 3 },

        new BottleRequest { from = "여행자 언니",
            body = "캠핑이 가고 싶은데 못 가고 있어요. 병 속에서라도 캠핑을 떠나게 해 주세요.",
            condition = "캠프파이어와 텐트를 모두 넣은 병", coins = 70, rewardItemId = "pinecone",
            thanks = "병 속 캠핑장이라니! 불멍하는 기분이에요. 고마워요.",
            test = r => Has(r, "campfire") && Has(r, "tent") },

        new BottleRequest { from = "조용한 할아버지",
            body = "복잡한 건 눈이 아파요. 딱 몇 개만 놓인, 단순하고 고요한 병이면 좋겠어요.",
            condition = "재료를 1~4개만 넣은 병", coins = 35, rewardItemId = "pebble",
            thanks = "비어 있는 곳이 오히려 마음을 편하게 하네요. 고마워요.",
            test = r => r.items.Count >= 1 && r.items.Count <= 4 },

        new BottleRequest { from = "책방 주인",
            body = "차 한 잔과 책 한 권이면 하루가 완성되죠. 그런 오후를 병에 담아 주세요.",
            condition = "찻잔과 책(또는 책 더미)을 넣은 병", coins = 55, rewardItemId = "candle",
            thanks = "향긋한 차 냄새가 나는 것 같아요. 오늘 오후는 이 병과 보낼게요.",
            test = r => Has(r, "teacup") && HasAny(r, "book", "book_stack") },

        new BottleRequest { from = "우산 가게 아주머니",
            body = "비 오는 날이 좋아요. 우산과 젖은 숲 느낌이 나는 병을 만들어 줄래요?",
            condition = "우산과 버섯, 이끼, 이슬방울 중 하나를 넣은 병", coins = 50, rewardItemId = "dew_drop",
            thanks = "빗소리가 들리는 것 같아요. 창밖에 비가 오면 더 좋겠네요.",
            test = r => Has(r, "umbrella") && HasAny(r, "mushroom", "brown_mushroom", "white_mushroom", "moss", "moss_dark", "dew_drop") },

        new BottleRequest { from = "겨울을 좋아하는 아이",
            body = "눈 오는 날에만 피는 꽃이 있대요! 눈꽃을 넣은 병을 보고 싶어요.",
            condition = "눈꽃을 2송이 이상 넣은 병", coins = 65, rewardItemId = "snow_flower",
            thanks = "차갑고 예쁜 꽃이네요. 병 안은 한겨울이에요!",
            test = r => CountIds(r, "snow_flower") >= 2 },

        new BottleRequest { from = "밤 산책하는 할머니",
            body = "여름밤에 날아다니던 반딧불이가 보고 싶어요. 반딧불이가 가득한 병을 부탁해도 될까요?",
            condition = "반딧불이를 3마리 이상 넣은 병", coins = 70, rewardItemId = "firefly",
            thanks = "반짝반짝, 어릴 때 보던 그 불빛이에요. 정말 고마워요.",
            test = r => CountIds(r, "firefly") >= 3 },

        new BottleRequest { from = "바닷가 수집가",
            body = "파도에 깎인 유리 조각이 너무 예뻐요. 바다 유리가 들어간 병을 갖고 싶어요.",
            condition = "바다 유리를 2개 이상 넣은 병", coins = 60, rewardItemId = "sea_glass",
            thanks = "햇빛에 비춰 보니 바다 조각 같아요. 보물이 하나 늘었네요.",
            test = r => CountIds(r, "sea_glass") >= 2 },

        new BottleRequest { from = "요정을 믿는 아이",
            body = "숲속에는 요정이 산대요. 요정의 집을 병 안에 만들어 줄 수 있어요?",
            condition = "요정 문과 풀(클로버, 이끼, 고사리) 중 하나를 넣은 병", coins = 60, rewardItemId = "four_leaf_clover",
            thanks = "요정이 정말 살 것 같아요! 문을 두드려 봐야겠어요.",
            test = r => Has(r, "fairy_door") && HasAny(r, "clover", "four_leaf_clover", "moss", "moss_dark", "fern") },

        new BottleRequest { from = "눈 마을 이장님",
            body = "눈 섬은 고요하고 포근하지요. 눈 마을의 정취가 느껴지는 병을 하나 보내 주시겠어요?",
            condition = "눈 섬 재료를 3개 이상 넣은 병", coins = 65, rewardItemId = "snowball",
            thanks = "마을 사람들이 모두 모여서 구경했어요. 따뜻한 마음이 전해져요.",
            test = r => Count(r, m => m.zone == MaterialZone.Snow) >= 3 },

        new BottleRequest { from = "벽난로 앞 할머니",
            body = "겨울엔 따뜻한 게 최고예요. 장작과 포근한 겨울 물건이 들어간 병을 갖고 싶어요.",
            condition = "장작과, 벙어리장갑 또는 겨울 방울을 넣은 병", coins = 60, rewardItemId = "winter_bell",
            thanks = "벽난로 곁에 두고 하루 종일 바라볼게요. 고마워요.",
            test = r => Has(r, "firewood") && HasAny(r, "mitten", "winter_bell") },
        new BottleRequest { from = "눈 내리는 날을 기다리는 아이",
            body = "병 속에도 눈이 내렸으면 좋겠어요! 눈이 펑펑 내리는 병을 보고 싶어요.",
            condition = "눈 내리기를 넣은 병", coins = 60, rewardItemId = "fx_snow",
            thanks = "와, 병 속에 진짜 눈이 내려요! 흔들지 않아도 계속 내려서 신기해요.",
            test = r => Has(r, "fx_snow") },

        new BottleRequest { from = "동물을 좋아하는 소녀",
            body = "작은 동물 친구가 있는 병이 갖고 싶어요. 새나 토끼, 고양이, 여우도 좋아요.",
            condition = "새, 토끼, 고양이, 여우 중 하나를 넣은 병", coins = 55, rewardItemId = "fx_petals",
            thanks = "귀여운 친구가 사는 병이에요! 매일 인사해야겠어요.",
            test = r => HasAny(r, "bird", "rabbit", "cat", "fox") },

        new BottleRequest { from = "바다를 그리워하는 선원",
            body = "작은 항구를 병에 담아 주세요. 배와 등대가 있으면 좋겠어요.",
            condition = "돛단배와 작은 등대를 넣은 병", coins = 70, rewardItemId = "fx_bubbles",
            thanks = "등대 불빛 아래 배가 떠 있네요. 먼 바다가 생각나요.",
            test = r => Has(r, "sailboat") && Has(r, "lighthouse") },
        new BottleRequest { from = "하늘을 좋아하는 소년",
            body = "하늘을 여행하는 기분을 느끼고 싶어요. 열기구가 떠 있는 병이 있으면 좋겠어요.",
            condition = "열기구와 종이배, 별 장식, 바람종 중 하나를 넣은 병", coins = 70, rewardItemId = "wind_chime",
            thanks = "구름 위를 날고 있는 것 같아요. 고마워요, 정원사님!",
            test = r => Has(r, "hot_air_balloon") && HasAny(r, "paper_boat", "star_ornament", "wind_chime") },
    };

    public static int Total => All.Length;

    public static BottleRequest Get(int id)
    {
        return id >= 0 && id < All.Length ? All[id] : null;
    }

    /// <summary>다음에 올 편지를 무작위로 고른다. 바로 전 편지와 같지 않게 한다.</summary>
    public static int Roll(int last)
    {
        for (int i = 0; i < 20; i++)
        {
            int pick = UnityEngine.Random.Range(0, All.Length);
            if (pick != last) return pick;
        }
        return 0;
    }

    /// <summary>이 병이 편지의 조건에 맞는가</summary>
    public static bool Matches(int requestId, BottleRecord record)
    {
        BottleRequest r = Get(requestId);
        return r != null && record != null && record.completed && r.test(record);
    }
}
