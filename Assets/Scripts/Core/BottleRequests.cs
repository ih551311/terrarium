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

    // 편지는 사람마다 말투와 사정이 다르게 쓴다. (모두 "~해 줄래요?" "고마워요"로 끝나면 같은 사람이 쓴 것처럼 읽힌다)
    private static readonly BottleRequest[] All =
    {
        new BottleRequest { from = "등대지기 할머니",
            body = "요즘 무릎이 쑤셔서 바닷가에 못 내려가요. 파도 소리가 나는 병이 하나 있으면 좋겠는데, 조개 같은 걸 넣어서요.",
            condition = "바닷가 재료를 3개 이상 넣은 병", coins = 40, rewardItemId = "seashell",
            thanks = "귀에 대 봤어요. 파도가 들려요. 늙은이 귀가 잘못된 건 아니겠지요.",
            test = r => Count(r, m => m.zone == MaterialZone.Beach) + (r.ground != null && r.ground.id == "sand" ? 1 : 0) >= 3 },

        new BottleRequest { from = "숲지기 아저씨",
            body = "비 온 뒤에 나던 이끼 냄새가 그립습니다. 이끼 낀 숲을 병에 좀 담아 주시겠어요.",
            condition = "숲 재료를 3개 이상 넣은 병", coins = 40, rewardItemId = "acorn",
            thanks = "창가에 두었어요. 아침에 눈을 뜨면 제일 먼저 보입니다.",
            test = r => Count(r, m => m.zone == MaterialZone.Forest) >= 3 },

        new BottleRequest { from = "꽃집 아가씨",
            body = "다음 주가 친구 생일인데 꽃다발은 너무 뻔해서요. 꽃이 잔뜩 들어간 병이면 어떨까 싶어요.",
            condition = "꽃을 4송이 이상 넣은 병", coins = 55, rewardItemId = "tulip",
            thanks = "친구가 한참 말이 없더니 웃었어요. 성공이에요.",
            test = r => Count(r, m => m.plantGroup == PlantGroup.Flower) >= 4 },

        new BottleRequest { from = "별을 모으는 아이",
            body = "저녁에 하늘을 보다가 생각났는데요, 밤도 병에 넣을 수 있어요? 달이랑 별이랑 불 켜진 것 같은 거요.",
            condition = "초, 별 장식, 초승달, 달맞이꽃, 반딧불이 중 2개 이상", coins = 60, rewardItemId = "star_ornament",
            thanks = "불 끄고 이불 속에서 봤어요. 진짜 밤이에요.",
            test = r => CountIds(r, "candle", "star_ornament", "crescent_moon", "evening_primrose", "firefly") >= 2 },

        new BottleRequest { from = "수집가 아저씨",
            body = "빈 데가 있으면 영 마음이 안 놓입니다. 꽉꽉 채운 병을 구경하고 싶어요.",
            condition = "재료를 10개 이상 넣은 병", coins = 60, rewardItemId = "crystal",
            thanks = "이만큼이나 들어가는군요. 돋보기까지 꺼내 봤습니다. 약소하지만 받아 주세요.",
            test = r => r.items.Count >= 10 },

        new BottleRequest { from = "모래성 아이",
            body = "나 모래성 만들다가 파도에 다 무너졌어. 병 속에는 모래 깔고 조개 놓아 주세요. 안 무너지게.",
            condition = "바닥을 모래로 하고 재료를 3개 이상 넣은 병", coins = 45, rewardItemId = "sand_dollar",
            thanks = "안 무너졌어요. 이게 제일 좋아요.",
            test = r => r.ground != null && r.ground.id == "sand" && r.items.Count >= 3 },

        new BottleRequest { from = "여행자 언니",
            body = "이번 달도 일이 밀려서 캠핑을 못 갔어요. 병 속에 텐트 치고 불 피워 주면 그걸로 버틸게요.",
            condition = "캠프파이어와 텐트를 모두 넣은 병", coins = 70, rewardItemId = "pinecone",
            thanks = "모닥불 보니까 고기가 먹고 싶네요. 잘 버틸게요.",
            test = r => Has(r, "campfire") && Has(r, "tent") },

        new BottleRequest { from = "조용한 할아버지",
            body = "요즘은 물건이 너무 많아서 정신이 없소. 몇 개만 덩그러니 놓인 병이 좋겠소.",
            condition = "재료를 1개에서 4개만 넣은 병", coins = 35, rewardItemId = "pebble",
            thanks = "비어 있는 게 눈이 편하구려. 오래 보겠소.",
            test = r => r.items.Count >= 1 && r.items.Count <= 4 },

        new BottleRequest { from = "책방 주인",
            body = "손님 없는 오후에는 차 한 잔에 책 한 권이 전부예요. 그런 오후를 병으로 보고 싶어서요.",
            condition = "찻잔과 책 또는 책 더미를 넣은 병", coins = 55, rewardItemId = "candle",
            thanks = "찻잔 옆에 두고 책을 폈는데 한 줄도 못 읽었어요. 병만 봤거든요.",
            test = r => Has(r, "teacup") && HasAny(r, "book", "book_stack") },

        new BottleRequest { from = "우산 가게 아주머니",
            body = "비 오는 날이 대목이라 좋긴 한데, 정작 젖는 건 싫어요. 우산이랑 촉촉한 숲이 같이 있는 병 어때요.",
            condition = "우산과 버섯, 이끼, 이슬방울 중 하나를 넣은 병", coins = 50, rewardItemId = "dew_drop",
            thanks = "비 안 오는 날에도 비 오는 기분이 나네요. 가게에 놓으면 장사가 되겠어요.",
            test = r => Has(r, "umbrella") && HasAny(r, "mushroom", "brown_mushroom", "white_mushroom", "moss", "moss_dark", "dew_drop") },

        new BottleRequest { from = "겨울을 좋아하는 아이",
            body = "눈 오는 날에만 피는 꽃 있잖아요. 그거 두 송이 넣은 병 보고 싶어요.",
            condition = "눈꽃을 2송이 이상 넣은 병", coins = 65, rewardItemId = "snow_flower",
            thanks = "차가워 보여요. 만져 보면 안 차가운데도요.",
            test = r => CountIds(r, "snow_flower") >= 2 },

        new BottleRequest { from = "밤 산책하는 할머니",
            body = "어릴 때는 개울가에 반딧불이가 많았는데 이젠 못 본 지 오래예요. 세 마리쯤 든 병이 있으면 좋겠네요.",
            condition = "반딧불이를 3마리 이상 넣은 병", coins = 70, rewardItemId = "firefly",
            thanks = "그때 그 빛이네요. 한동안 불을 못 끄겠어요.",
            test = r => CountIds(r, "firefly") >= 3 },

        new BottleRequest { from = "바닷가 수집가",
            body = "바다 유리는 같은 게 하나도 없어서 좋아요. 두 개 든 병이면 제 서랍이 한 칸 늘겠어요.",
            condition = "바다 유리를 2개 이상 넣은 병", coins = 60, rewardItemId = "sea_glass",
            thanks = "햇빛에 대 봤는데 색이 다 달라요. 그래서 줍는다니까요.",
            test = r => CountIds(r, "sea_glass") >= 2 },

        new BottleRequest { from = "요정을 믿는 아이",
            body = "엄마는 아니래요. 그래도 숲에는 요정이 살아요. 문이랑 풀을 놓아서 요정 집 좀 만들어 주세요.",
            condition = "요정 문과 클로버, 이끼, 고사리 중 하나를 넣은 병", coins = 60, rewardItemId = "four_leaf_clover",
            thanks = "밤에 문을 두드려 봤어요. 대답은 안 했지만 있는 것 같아요.",
            test = r => Has(r, "fairy_door") && HasAny(r, "clover", "four_leaf_clover", "moss", "moss_dark", "fern") },

        new BottleRequest { from = "눈 마을 이장님",
            body = "눈 마을 풍경이 담긴 병이 하나 있었으면 합니다. 마을 회관에 놓아두려고요.",
            condition = "눈 섬 재료를 3개 이상 넣은 병", coins = 65, rewardItemId = "snowball",
            thanks = "회관에 놓았더니 다들 한 번씩 들여다보고 갑니다.",
            test = r => Count(r, m => m.zone == MaterialZone.Snow) >= 3 },

        new BottleRequest { from = "벽난로 앞 할머니",
            body = "벽난로 앞에 앉으면 다른 건 아무것도 안 하게 돼요. 장작이랑 따뜻한 털장갑이 든 병을 곁에 두고 싶어요.",
            condition = "장작과, 벙어리장갑 또는 겨울 방울을 넣은 병", coins = 60, rewardItemId = "winter_bell",
            thanks = "벽난로 옆 선반에 놓았어요. 같이 졸고 있네요.",
            test = r => Has(r, "firewood") && HasAny(r, "mitten", "winter_bell") },

        new BottleRequest { from = "눈 내리는 날을 기다리는 아이",
            body = "올해는 눈이 안 와요. 병 안에라도 눈이 내렸으면 좋겠어요.",
            condition = "눈 내리기를 넣은 병", coins = 60, rewardItemId = "fx_snow",
            thanks = "계속 내려요. 흔들지도 않았는데.",
            test = r => Has(r, "fx_snow") },

        new BottleRequest { from = "동물을 좋아하는 소녀",
            body = "우리 집은 동물을 못 키워요. 병 속에 새나 토끼나 고양이나 여우가 있으면 매일 볼 수 있잖아요.",
            condition = "새, 토끼, 고양이, 여우 중 하나를 넣은 병", coins = 55, rewardItemId = "fx_petals",
            thanks = "이름을 붙였어요. 비밀이에요.",
            test = r => HasAny(r, "bird", "rabbit", "cat", "fox") },

        new BottleRequest { from = "바다를 그리워하는 선원",
            body = "배에서 내린 지 석 달이네. 작은 항구를 병에 하나 만들어 주게. 배랑 등대면 충분하네.",
            condition = "돛단배와 작은 등대를 넣은 병", coins = 70, rewardItemId = "fx_bubbles",
            thanks = "등대 불빛이 아직 켜져 있는 것 같군. 잘 받았네.",
            test = r => Has(r, "sailboat") && Has(r, "lighthouse") },

        new BottleRequest { from = "하늘을 좋아하는 소년",
            body = "하늘을 날아 보고 싶어서 열기구 그림만 그리고 있어요. 병 속에 열기구가 둥둥 떠 있으면 좋겠어요.",
            condition = "열기구와 종이배, 별 장식, 바람종 중 하나를 넣은 병", coins = 70, rewardItemId = "wind_chime",
            thanks = "구름 위에 있는 것 같아요. 정원사님도 타고 싶지 않아요?",
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
