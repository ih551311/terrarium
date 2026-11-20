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

        // ───── 아래는 나중에 더 쓴 편지들 (번호는 저장에 쓰이니 앞의 편지 순서를 바꾸지 말고 뒤에만 덧붙인다) ─────

        new BottleRequest { from = "사막을 건너온 여행자",
            body = "모래 언덕을 사흘 걸었어요. 이제는 어디서든 푹신한 바닥이 그리워요. 그 모래를 병에 담아 두면 가끔 꺼내 볼 수 있겠죠.",
            condition = "사막 재료를 3개 이상 넣은 병", coins = 55, rewardItemId = "sandstone",
            thanks = "신발 속에 모래가 아직 있는데도 이 병이 더 모래 같아요.",
            test = r => Count(r, m => m.zone == MaterialZone.Desert) + (r.ground != null && r.ground.id == "red_sand" ? 1 : 0) >= 3 },

        new BottleRequest { from = "발굴하는 아주머니",
            body = "화석이랑 깨진 도자기가 한 자리에서 나왔어요. 둘이 옛날에 같이 있던 사이일지도 몰라서, 병 속에서라도 다시 같이 두고 싶어요.",
            condition = "화석과 옛 도자기 조각을 모두 넣은 병", coins = 75, rewardItemId = "fossil",
            thanks = "붓으로 먼지를 털어 주고 싶어졌어요. 그런 병은 처음이에요.",
            test = r => Has(r, "fossil") && Has(r, "pottery_shard") },

        new BottleRequest { from = "선인장을 키우는 소년",
            body = "선인장은 물을 안 줘도 된다고 해서 샀는데 정말 안 줘도 되니까 심심해요. 병 속 선인장에 꽃이 핀 걸 보면 덜 심심할 것 같아요.",
            condition = "선인장과 선인장 꽃을 모두 넣은 병", coins = 60, rewardItemId = "cactus_flower",
            thanks = "꽃이 핀 날은 제 생일 같아요. 병에서도요.",
            test = r => Has(r, "cactus_piece") && Has(r, "cactus_flower") },

        new BottleRequest { from = "낙타 상인",
            body = "장사가 잘되는 날은 낙타 등이 가볍고 안 되는 날은 무겁소. 오늘은 무겁소. 낙타랑 천막이 있는 병을 보면 좀 가벼워질 것 같구려.",
            condition = "낙타와 사막 천막을 모두 넣은 병", coins = 75, rewardItemId = "desert_rose",
            thanks = "낙타 녀석이 병을 보고 코를 킁킁거리오. 아는 체를 하는 모양이오.",
            test = r => Has(r, "camel") && Has(r, "desert_tent") },

        new BottleRequest { from = "피라미드를 그리는 학생",
            body = "숙제로 피라미드를 그려야 하는데 자꾸 삼각형 세 개가 되어 버려요. 모래 위에 서 있는 진짜 같은 걸 보면 좀 나아질 것 같아요.",
            condition = "작은 피라미드를 넣고 붉은 모래 바닥이거나 사암 조각도 넣은 병", coins = 70, rewardItemId = "pyramid",
            thanks = "그렸어요. 삼각형이 네 개였는데 선생님이 맞다고 했어요.",
            test = r => Has(r, "pyramid") && ((r.ground != null && r.ground.id == "red_sand") || Has(r, "sandstone")) },

        new BottleRequest { from = "얼음낚시 하는 할아버지",
            body = "구멍 앞에 앉아 있는 시간이 좋아서 가는 거지 잡으려고 가는 건 아니라오. 그래도 잡힌 물고기를 병에 넣어 두면 자랑은 되겠지.",
            condition = "물고기를 2마리 이상 넣은 병", coins = 70, rewardItemId = "smelt",
            thanks = "마을 노인정에 들고 갔다오. 다들 어디서 잡았냐고 묻는데 안 알려 줬소.",
            test = r => CountIds(r, "goldfish", "ice_trout", "smelt", "aurora_fish") >= 2 },

        new BottleRequest { from = "밤하늘을 좋아하는 화가",
            body = "오로라는 한 번도 못 봤어요. 눈 덮인 땅에 오로라를 닮은 물고기가 있다고 들었는데, 병에 넣어 주시면 그림으로 그려 볼게요.",
            condition = "눈 바닥에 오로라 물고기를 넣은 병", coins = 85, rewardItemId = "aurora_fish",
            thanks = "붓이 저절로 움직여요. 이번 그림은 제목을 못 정하겠어요.",
            test = r => r.ground != null && r.ground.id == "snow" && Has(r, "aurora_fish") },

        new BottleRequest { from = "비를 좋아하는 아이",
            body = "엄마는 비 오면 빨래 걱정만 해요. 나는 비 오는 날이 제일 좋은데. 병 속에 비가 내리면 빨래 걱정 안 해도 되잖아요.",
            condition = "보슬비와 우산 또는 청개구리를 넣은 병", coins = 55, rewardItemId = "dew_drop",
            thanks = "우산 쓰고 병을 봤어요. 이상한 아이 같지만 괜찮아요.",
            test = r => Has(r, "fx_rain") && HasAny(r, "umbrella", "tree_frog") },

        new BottleRequest { from = "천둥을 무서워하는 할머니",
            body = "번개가 치면 이불 속으로 들어가는 게 버릇이 됐어요. 번개가 굳어서 유리가 됐다는 얘기를 들었는데, 그런 건 무섭지 않을 것 같아요.",
            condition = "번개 유리를 넣은 병", coins = 90, rewardItemId = "lightning_glass",
            thanks = "만져 봤어요. 하나도 안 무서워요. 오히려 예뻐서 놀랐어요.",
            test = r => Has(r, "lightning_glass") },

        new BottleRequest { from = "무지개를 쫓는 아이",
            body = "무지개 끝에는 보물이 있다고 해서 쫓아갔는데 끝이 없었어요. 대신 무지개 색 조약돌을 보면 좀 위로가 될 것 같아요.",
            condition = "무지개 조약돌을 넣은 병", coins = 85, rewardItemId = "rainbow_pebble",
            thanks = "보물은 이미 찾았네요. 이거예요.",
            test = r => Has(r, "rainbow_pebble") },

        new BottleRequest { from = "바람 부는 날을 기다리는 사람",
            body = "민들레 홀씨가 날아가는 걸 보려고 바람 부는 날을 기다렸어요. 그런데 막상 바람이 부니 홀씨가 다 날아가 버렸지 뭐예요. 병 속에 깃털이랑 같이 잡아 둘 수 있을까요.",
            condition = "민들레 홀씨와 바람 깃털을 모두 넣은 병", coins = 75, rewardItemId = "wind_feather",
            thanks = "이제 안 날아가요. 그런데 이상하게 조금 쓸쓸해 보이기도 해요.",
            test = r => Has(r, "dandelion_puff") && Has(r, "wind_feather") },

        new BottleRequest { from = "안개 낀 아침에 일하는 우유 배달부",
            body = "새벽 안개가 낀 날은 길이 안 보여서 느리게 가야 해요. 그래도 거미줄에 이슬이 맺힌 걸 보는 건 이 일의 덤이에요.",
            condition = "안개꽃과 이슬 거미줄을 모두 넣은 병", coins = 75, rewardItemId = "spider_web",
            thanks = "아침에 문 앞에 놓고 갔다가 다시 가져왔어요. 아까워서요.",
            test = r => Has(r, "mist_flower") && Has(r, "spider_web") },

        new BottleRequest { from = "풍경을 찍는 사진가",
            body = "삼각대는 챙겼는데 자꾸 렌즈를 두고 와요. 망원경이 있는 병이면 멀리 있는 걸 보는 기분은 낼 수 있겠어요. 이정표도 하나 있으면 길을 잃은 느낌까지 나겠고요.",
            condition = "망원경과 이정표를 모두 넣은 병", coins = 60, rewardItemId = "signpost",
            thanks = "필름 한 통을 병 하나에 다 썼어요. 후회는 없어요.",
            test = r => Has(r, "telescope") && Has(r, "signpost") },

        new BottleRequest { from = "공원 관리인",
            body = "분수 옆 벤치에 앉아서 도시락 먹는 사람들을 보는 게 제 낙입니다. 그 풍경을 병에 좀 담아 주세요. 나무나 풀도 같이요.",
            condition = "벤치와 분수와 식물 2개 이상을 넣은 병", coins = 70, rewardItemId = "bench",
            thanks = "도시락을 싸 왔는데 병 앞에서 먹었습니다. 규정 위반은 아니죠.",
            test = r => Has(r, "bench") && Has(r, "fountain") && Count(r, m => m.kind == MaterialKind.Plant || m.kind == MaterialKind.Cover) >= 2 },

        new BottleRequest { from = "다리를 놓는 석공",
            body = "강이 없는 마을에 다리를 놓아 달라는 주문이 들어왔네. 말이 되나 싶지만 일은 일이니 돌다리 모양이 어떨지 병으로 먼저 보고 싶네.",
            condition = "돌다리와 조약돌 또는 분수를 넣은 병", coins = 60, rewardItemId = "pebble",
            thanks = "이 정도면 되겠군. 강은 나중에 누가 파겠지.",
            test = r => Has(r, "stone_bridge") && HasAny(r, "pebble", "fountain") },

        new BottleRequest { from = "연을 만드는 소년",
            body = "연은 바람이 없으면 그냥 종이예요. 그래서 바람 날개를 단 연이 있으면 좋겠어요. 병 속에서는 바람이 늘 있으니까요.",
            condition = "연과 바람 깃털, 민들레 홀씨, 바람종 중 하나를 넣은 병", coins = 60, rewardItemId = "kite",
            thanks = "꼬리가 흔들려요. 정말로요. 바람종도 울리고요.",
            test = r => Has(r, "kite") && HasAny(r, "wind_feather", "dandelion_puff", "wind_chime") },

        new BottleRequest { from = "별을 세는 캠핑 동호회",
            body = "회원이 둘뿐이라 모임이 자주 취소돼요. 그래서 병 속에서라도 모닥불 피우고 별을 세고 싶어요. 장작 타는 소리는 상상할게요.",
            condition = "캠프파이어와 별 장식 또는 초승달을 넣은 병", coins = 75, rewardItemId = "star_ornament",
            thanks = "서른까지 세다가 졸았어요. 병이 별똥별 하나를 보내 준 것 같아요.",
            test = r => Has(r, "campfire") && HasAny(r, "star_ornament", "crescent_moon") },

        new BottleRequest { from = "지붕 위의 고양이를 둔 사람",
            body = "우리 고양이는 사다리만 보면 올라가요. 내려오는 건 못 해서 제가 매번 데리러 가고요. 사다리와 고양이가 한 병에 있으면 이젠 데리러 안 가도 되겠죠.",
            condition = "고양이와 사다리를 모두 넣은 병", coins = 65, rewardItemId = "ladder",
            thanks = "진짜 고양이가 병을 한참 쳐다봐요. 자기 분신인 줄 아나 봐요.",
            test = r => Has(r, "cat") && Has(r, "ladder") },

        new BottleRequest { from = "차를 좋아하는 집사",
            body = "고양이를 무릎에 올리고 차를 마시는 게 소원이에요. 고양이는 무릎보다 찻잔을 더 좋아하지만요.",
            condition = "고양이와 찻잔을 모두 넣은 병", coins = 60, rewardItemId = "candle",
            thanks = "찻잔 옆에서 졸고 있는 고양이를 보니 정말 제 일상 같아요.",
            test = r => Has(r, "cat") && Has(r, "teacup") },

        new BottleRequest { from = "토끼를 기다리는 아이",
            body = "클로버밭에 토끼가 나온다고 해서 한참 앉아 있었는데 다리만 저렸어요. 병 속 클로버밭에는 토끼가 꼭 나오게 해 주세요.",
            condition = "토끼와 클로버 또는 네잎클로버를 넣은 병", coins = 60, rewardItemId = "clover",
            thanks = "네잎클로버를 찾은 건 병 속 토끼가 먼저예요. 부러워요.",
            test = r => Has(r, "rabbit") && HasAny(r, "clover", "four_leaf_clover") },

        new BottleRequest { from = "여우 이야기를 쓰는 작가",
            body = "여우가 주인공인 동화를 쓰는데 숲 장면이 영 안 써져요. 도토리나 솔방울이 굴러다니는 숲에 여우가 있는 병을 보면 문장이 나올 것 같아요.",
            condition = "여우와 도토리 또는 솔방울과 이끼를 넣은 병", coins = 65, rewardItemId = "pinecone",
            thanks = "첫 문장을 썼어요. 여우는 도토리를 줍지 않았다. 그게 시작이에요.",
            test = r => Has(r, "fox") && HasAny(r, "acorn", "pinecone") && HasAny(r, "moss", "moss_dark") },

        new BottleRequest { from = "새집을 지어 주는 할아버지",
            body = "새들은 집을 알아서 짓는데 나는 자꾸 지어 주고 싶구려. 나뭇가지와 새를 한 병에 넣어 주면 오늘도 한 채 지은 기분이 들겠소.",
            condition = "작은 새와 나뭇가지를 넣은 병", coins = 55, rewardItemId = "twig",
            thanks = "빈 새집도 하나 샀소. 병 속 녀석이 이사 오면 좋겠구려.",
            test = r => Has(r, "bird") && Has(r, "twig") },

        new BottleRequest { from = "버섯 마을을 그리는 아이",
            body = "버섯 집에는 버섯 이웃이 있어야 해요. 이웃이 없으면 심심하잖아요. 두세 개쯤 같이 있는 병이 좋겠어요.",
            condition = "버섯 집과 버섯류 2개 이상을 넣은 병", coins = 65, rewardItemId = "white_mushroom",
            thanks = "이름도 지어 줬어요. 몽글이랑 뭉글이랑 또 몽글이요.",
            test = r => Has(r, "mushroom_house") && CountIds(r, "mushroom", "blue_mushroom", "brown_mushroom", "white_mushroom") >= 2 },

        new BottleRequest { from = "비 온 뒤 숲을 걷는 사람",
            body = "비 그친 숲에는 푸른 버섯이 핀다더군요. 한 번 보고 싶은데 늘 한발 늦어요. 개구리가 같이 있다면 더 좋겠어요.",
            condition = "푸른 버섯과 청개구리를 모두 넣은 병", coins = 85, rewardItemId = "blue_mushroom",
            thanks = "개구리가 울 것 같은 병이에요. 숨죽이고 기다리는 중이에요.",
            test = r => Has(r, "blue_mushroom") && Has(r, "tree_frog") },

        new BottleRequest { from = "청개구리를 좋아하는 소녀",
            body = "엄마 말을 반대로 하는 개구리 이야기 알아요? 나랑 닮아서 좋아해요. 촉촉한 이끼 위에 앉아 있는 개구리를 부탁드려요.",
            condition = "청개구리와 이끼, 빗물 이끼, 이슬방울 중 하나를 넣은 병", coins = 70, rewardItemId = "rain_moss",
            thanks = "개구리한테 반대로 말하는 연습을 시켜 봤어요. 말은 안 들어요.",
            test = r => Has(r, "tree_frog") && HasAny(r, "moss", "moss_dark", "rain_moss", "dew_drop") },

        new BottleRequest { from = "아침을 모으는 아이",
            body = "아침 풀잎에 달린 이슬이 제일 예뻐요. 해가 뜨면 사라져서 아쉬워요. 병에 두 방울쯤 담아 두면 해가 떠도 안 사라지겠죠?",
            condition = "이슬방울을 2개 이상 넣은 병", coins = 60, rewardItemId = "dew_drop",
            thanks = "밤새 지켜봤는데 정말 안 사라졌어요. 졸려서 눈은 감겼지만요.",
            test = r => CountIds(r, "dew_drop") >= 2 },

        new BottleRequest { from = "해바라기를 좋아하는 할머니",
            body = "영감이 해바라기 씨를 좋아해서 매년 심었다우. 올해는 심을 사람이 없으니 병으로 대신해 주겠수. 두 송이만이라도.",
            condition = "해바라기를 2송이 이상 넣은 병", coins = 60, rewardItemId = "sunflower",
            thanks = "고개를 이쪽으로 돌린 것 같구려. 내가 보여서 그런가 보우.",
            test = r => CountIds(r, "sunflower") >= 2 },

        new BottleRequest { from = "보라색만 입는 언니",
            body = "옷장이 온통 보라색이라 다들 놀라는데, 병도 보라색이면 완벽하겠어요. 라벤더나 보라 들꽃 같은 걸로요.",
            condition = "라벤더, 보라 들꽃, 파란 들꽃을 합쳐서 3송이 이상 넣은 병", coins = 65, rewardItemId = "lavender",
            thanks = "옷이랑 병이 똑같은 색이에요. 이제 병도 같이 외출해야겠어요.",
            test = r => CountIds(r, "lavender", "purple_flower", "blue_flower") >= 3 },

        new BottleRequest { from = "가을을 좋아하는 아저씨",
            body = "가을이 짧아서 아쉽습니다. 단풍잎이랑 도토리랑 솔방울이 굴러다니는 병이 있으면 가을을 좀 늘릴 수 있을 것 같습니다.",
            condition = "단풍잎과 도토리, 솔방울을 모두 넣은 병", coins = 60, rewardItemId = "orange_leaf",
            thanks = "책상 위에 두고 일하니 계절이 길어진 느낌입니다. 효과가 있습니다.",
            test = r => Has(r, "orange_leaf") && Has(r, "acorn") && Has(r, "pinecone") },

        new BottleRequest { from = "갈대밭을 거닐던 시인",
            body = "바닷가 갈대가 바람에 눕는 걸 보며 시 한 줄을 건졌는데 집에 오는 길에 잊어버렸어요. 모래와 갈대가 있는 병이면 그 줄이 돌아올지도 몰라요.",
            condition = "모래 바닥에 갈대를 2개 이상 넣은 병", coins = 55, rewardItemId = "reed",
            thanks = "한 줄이 돌아왔어요. 그런데 뜻은 잘 모르겠어요. 그래도 좋아요.",
            test = r => r.ground != null && r.ground.id == "sand" && CountIds(r, "reed") >= 2 },

        new BottleRequest { from = "허수아비를 세우는 농부",
            body = "올해 밀이 잘 됐는데 참새가 먼저 알았소. 허수아비가 영 시원찮아서 말이오. 밀 이삭이 가득한 병을 보면 마음이 좀 놓일 것 같소.",
            condition = "밀 이삭을 3개 이상 넣은 병", coins = 50, rewardItemId = "wheat",
            thanks = "병 속 밀은 참새가 안 오니 걱정 없구려. 부럽소.",
            test = r => CountIds(r, "wheat") >= 3 },

        new BottleRequest { from = "눈 마을의 목수",
            body = "겨울에는 일이 없어서 오두막 모형이나 만들고 있소. 눈이 내리는 병 속에 오두막이 서 있으면 그게 제일 좋은 견본이 되겠소.",
            condition = "눈 오두막과 눈 내리기 또는 눈 바닥을 넣은 병", coins = 80, rewardItemId = "fx_snow",
            thanks = "지붕 모양을 다시 고쳤소. 눈이 쌓이는 걸 보고 배웠소.",
            test = r => HasAny(r, "snow_cottage1", "snow_cottage2", "snow_cottage3") && (Has(r, "fx_snow") || (r.ground != null && r.ground.id == "snow")) },

        new BottleRequest { from = "눈사람을 만드는 아이들",
            body = "우리 반 눈사람 대회에서 꼴찌를 했어요. 내년에는 꼭 이길 거라서 연구하려고요. 눈덩이가 여러 개 있고 눈사람도 하나 있는 병이면 돼요.",
            condition = "미니 눈사람과 눈덩이 2개를 넣은 병", coins = 65, rewardItemId = "snowball",
            thanks = "우리 눈사람이 훨씬 못생겼네요. 그래도 우리 거라서 좋아요.",
            test = r => Has(r, "mini_snowman") && CountIds(r, "snowball") >= 2 },

        new BottleRequest { from = "빙판 위의 아이",
            body = "겨울엔 호수가 얼어서 걸어 다닐 수 있어요. 얼음 조각이랑 고드름이 번쩍번쩍한 병이면 호수가 생각날 것 같아요.",
            condition = "얼음 조각과 고드름을 모두 넣은 병", coins = 60, rewardItemId = "ice_shard",
            thanks = "얼음이 그대로예요. 녹을 줄 알았는데. 신기해요.",
            test = r => Has(r, "ice_shard") && Has(r, "icicle") },

        new BottleRequest { from = "눈 마을 우물가 아주머니",
            body = "우물에서 물 긷는 시간이 하루 중 제일 한가해요. 우물 옆에 오두막이 있는 풍경이면 딱이에요.",
            condition = "눈 우물과 눈 오두막을 모두 넣은 병", coins = 75, rewardItemId = "snow_well",
            thanks = "두레박 소리가 들리는 것 같아요. 제 귀가 또 그러네요.",
            test = r => Has(r, "snow_well") && HasAny(r, "snow_cottage1", "snow_cottage2", "snow_cottage3") },

        new BottleRequest { from = "항구에서 기다리는 아이",
            body = "아빠 배가 곧 들어와요. 아직 안 보이지만 종이배를 접어 두고 기다리는 중이에요. 진짜 배랑 종이배가 나란히 있으면 좋겠어요.",
            condition = "돛단배와 종이배를 모두 넣은 병", coins = 60, rewardItemId = "paper_boat",
            thanks = "아빠가 돌아왔어요. 병 보고 웃었어요. 둘이 같이 봤어요.",
            test = r => Has(r, "sailboat") && Has(r, "paper_boat") },

        new BottleRequest { from = "하나만 좋아하는 사람",
            body = "여러 개는 고르기 힘들어서 하나만 있는 게 좋아요. 정말 딱 하나만 든 병이면 어떤 물건이든 상관없어요.",
            condition = "재료를 딱 1개만 넣은 병", coins = 50, rewardItemId = "pebble",
            thanks = "이게 제일 마음에 들어요. 고민이 없는 병이라서요.",
            test = r => r.items.Count == 1 },

        new BottleRequest { from = "도감을 만드는 학자",
            body = "세상에 같은 재료 두 개는 필요 없다고 생각하오. 최대한 많은 종류가 한 병에 들어 있는 걸 보면 도감 만드는 데 큰 도움이 되겠소.",
            condition = "서로 다른 재료를 7종류 이상 넣은 병", coins = 85, rewardItemId = "amber",
            thanks = "종류별로 번호를 매겨 봤소. 이 병만으로 한 쪽이 다 찼소.",
            test = r => Distinct(r) >= 7 },

        new BottleRequest { from = "도서관 야간 사서",
            body = "문 닫은 도서관에서 촛불 켜고 책 읽는 게 몰래 하는 낙이에요. 달도 하나 떠 있으면 아주 완벽한 밤이겠어요.",
            condition = "책 또는 책 더미와 초, 초승달을 모두 넣은 병", coins = 70, rewardItemId = "book",
            thanks = "병 속 책은 안 읽혀요. 읽는 척만 하게 되네요.",
            test = r => HasAny(r, "book", "book_stack") && Has(r, "candle") && Has(r, "crescent_moon") },

        new BottleRequest { from = "약초를 캐는 아주머니",
            body = "약초는 같이 있으면 서로 약이 되는 게 있어요. 라벤더랑 클로버랑 고사리를 한데 모은 병이면 제 약방 선반에 올려놓기 좋겠어요.",
            condition = "라벤더, 클로버, 고사리를 모두 넣은 병", coins = 65, rewardItemId = "fern",
            thanks = "향이 날 것 같은 병이에요. 손님들이 약인 줄 알고 사려고 해서 곤란해요.",
            test = r => Has(r, "lavender") && Has(r, "clover") && Has(r, "fern") },

        new BottleRequest { from = "꽃과 새를 그리는 화가",
            body = "꽃만 그리면 허전하고 새만 그리면 외로워요. 꽃이 몇 송이 있고 그 위에 새나 토끼가 같이 있는 병이면 그림이 완성돼요.",
            condition = "꽃 2송이 이상과 새, 토끼, 고양이, 여우 중 하나를 넣은 병", coins = 65, rewardItemId = "white_flower",
            thanks = "캔버스가 작아서 병이 더 커 보여요. 어쩌죠.",
            test = r => Count(r, m => m.plantGroup == PlantGroup.Flower) >= 2 && HasAny(r, "bird", "rabbit", "cat", "fox") },

        new BottleRequest { from = "별빛 아래 모래사장을 걷는 사람",
            body = "한여름 밤바다는 별도 많고 모래도 포근해요. 모래 위에 별과 달이 같이 있는 병을 만들어 주시면 소원을 하나 빌게요.",
            condition = "모래 바닥에 별 장식과 초승달을 모두 넣은 병", coins = 70, rewardItemId = "crescent_moon",
            thanks = "빌었어요. 말하면 안 이루어진다니까 비밀이에요.",
            test = r => r.ground != null && r.ground.id == "sand" && Has(r, "star_ornament") && Has(r, "crescent_moon") },

        new BottleRequest { from = "겨울 장식을 하는 가게 주인",
            body = "연말이라 가게 진열창을 꾸며야 해요. 솔잎 가지와 호랑가시 열매에 방울이 같이 있으면 분위기가 딱 날 것 같아요.",
            condition = "솔잎 가지와 호랑가시 열매와 겨울 방울을 모두 넣은 병", coins = 75, rewardItemId = "pine_sprig",
            thanks = "진열창에 놓았더니 들어오는 손님마다 한마디씩 해요. 장사가 되려나 봐요.",
            test = r => Has(r, "pine_sprig") && Has(r, "holly_berry") && Has(r, "winter_bell") },
    };
    public static int Total => All.Length;

    private static int Distinct(BottleRecord r)
    {
        var seen = new HashSet<string>();
        foreach (PlacedData p in r.items) if (p.material != null) seen.Add(p.material.id);
        return seen.Count;
    }

    // 최근에 온 편지는 한동안 다시 오지 않게 기억해 둔다 (게임을 다시 켜면 비워진다)
    private static readonly List<int> recent = new List<int>();

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        recent.Clear();
    }

    public static BottleRequest Get(int id)
    {
        return id >= 0 && id < All.Length ? All[id] : null;
    }

    /// <summary>다음에 올 편지를 무작위로 고른다. 바로 전 편지와 같지 않게 한다.</summary>
    public static int Roll(int last)
    {
        if (last >= 0 && !recent.Contains(last)) recent.Add(last);
        int keep = Math.Min(12, All.Length - 2); // 최근 열두 통은 건너뛴다
        while (recent.Count > keep) recent.RemoveAt(0);

        for (int i = 0; i < 60; i++)
        {
            int pick = UnityEngine.Random.Range(0, All.Length);
            if (pick != last && !recent.Contains(pick)) return pick;
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
