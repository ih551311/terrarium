using UnityEditor;
using UnityEngine;

/// <summary>
/// 재료 데이터(기본 재료 14종 + 희귀 2종 + 상점 소품 16종 + 바닥재 모래)를 만들고,
/// 각 재료에 모델과 "어디서 어떻게 구하는가", 코인 값을 연결한다.
/// 메뉴: 유리병 정원 > 재료 데이터 만들기. 여러 번 눌러도 같은 결과가 나온다.
/// 어떤 재료를 어디서 구할지는 아래 표(Entries)만 고치면 된다.
/// </summary>
public static class MaterialSetup
{
    private const string DataFolder = "Assets/Data/Materials";
    private const string ModelFolder = "Assets/Models/Deco";

    private struct Entry
    {
        public string id, displayName, model;
        public MaterialKind kind;
        public MaterialZone zone;
        public AcquireMethod acquire;
        public MaterialCondition condition;
        public bool rare;
        public int sell, buy;
        public Color color; // 모델이 없을 때와 미리보기(그림자)에 쓰는 색

        public Entry(string id, string displayName, MaterialKind kind, string model, Color color,
                     MaterialZone zone, AcquireMethod acquire, int sell, int buy = 0,
                     MaterialCondition condition = MaterialCondition.Always, bool rare = false)
        {
            this.id = id; this.displayName = displayName; this.kind = kind; this.model = model; this.color = color;
            this.zone = zone; this.acquire = acquire; this.sell = sell; this.buy = buy;
            this.condition = condition; this.rare = rare;
        }
    }

    private const MaterialKind Plant = MaterialKind.Plant;
    private const MaterialKind Deco = MaterialKind.Decoration;
    private const MaterialZone Forest = MaterialZone.Forest, Beach = MaterialZone.Beach, Hill = MaterialZone.FlowerHill, Shop = MaterialZone.Shop, Snow = MaterialZone.Snow, Desert = MaterialZone.Desert;
    private const AcquireMethod Hand = AcquireMethod.Hand, Axe = AcquireMethod.Axe, Pick = AcquireMethod.Pickaxe, Buy = AcquireMethod.Shop, Hoe = AcquireMethod.Hoe;

    // model은 Assets/Models/Deco 안의 파일 이름(확장자 없이). 가격은 모두 임시 값이다.
    private static readonly Entry[] Entries =
    {
        // ── 작은 숲 (처음부터)
        new Entry("moss",             "이끼",       MaterialKind.Cover, "deco_moss_light",    new Color(0.44f, 0.64f, 0.36f), Forest, Hand, 1),
        new Entry("fern",             "고사리",     Plant, "deco_fern",          new Color(0.45f, 0.70f, 0.42f), Forest, Hand, 1),
        new Entry("twig",             "나뭇가지",   Deco,  "deco_twig",          new Color(0.70f, 0.55f, 0.35f), Forest, Axe,  1),
        new Entry("pinecone",         "솔방울",     Deco,  "deco_pine_cone",     new Color(0.55f, 0.35f, 0.20f), Forest, Axe,  1),
        new Entry("mushroom",         "버섯",       Deco,  "deco_mushroom_red",  new Color(0.85f, 0.35f, 0.33f), Forest, Hand, 2, 0, MaterialCondition.AfterRain),
        // ── 바닷가 (병 2개 완성 후)
        new Entry("seashell",         "조개껍데기", Deco,  "deco_seashell",      new Color(0.96f, 0.82f, 0.79f), Beach, Hand, 2),
        new Entry("scallop",          "가리비",     Deco,  "deco_scallop",       new Color(0.96f, 0.68f, 0.52f), Beach, Hand, 2),
        new Entry("clam",             "대합",       Deco,  "deco_clam",          new Color(0.72f, 0.60f, 0.88f), Beach, Hand, 2),
        new Entry("spiral_shell",     "고둥",       Deco,  "deco_spiral_shell",  new Color(0.94f, 0.60f, 0.40f), Beach, Hand, 2),
        new Entry("sand_dollar",      "모래 달러",  Deco,  "deco_sand_dollar",   new Color(0.89f, 0.78f, 0.61f), Beach, Hand, 3),
        new Entry("turret_shell",     "뿔고둥",     Deco,  "deco_turret_shell",  new Color(0.77f, 0.61f, 0.44f), Beach, Hand, 3),
        new Entry("pebble",           "조약돌",    Deco,  "deco_pebble_gray",   new Color(0.62f, 0.62f, 0.65f), Beach, Pick, 1),
        new Entry("driftwood",        "유목",       Deco,  "deco_driftwood",     new Color(0.69f, 0.51f, 0.35f), Beach, Axe,  2),
        // 모래는 바닥재라서 병 안에 놓는 모델이 없다 (BottleSetup이 데이터를 만들고, 구역은 아래에서 정한다)
        // ── 꽃 언덕 (병 5개 완성 후)
        new Entry("white_flower",     "흰 들꽃",    Plant, "deco_flower_white",  new Color(0.96f, 0.95f, 0.92f), Hill, Hand, 2),
        new Entry("blue_flower",      "파란 들꽃",  Plant, "deco_flower_blue",   new Color(0.49f, 0.66f, 0.89f), Hill, Hand, 2),
        new Entry("clover",           "클로버",     Plant, "deco_clover",        new Color(0.64f, 0.83f, 0.51f), Hill, Hand, 1),
        new Entry("small_rock",       "작은 바위",  Deco,  "deco_rock_small",    new Color(0.62f, 0.62f, 0.65f), Hill, Pick, 2),
        new Entry("evening_primrose", "달맞이꽃",   Plant, "deco_flower_yellow", new Color(0.97f, 0.85f, 0.40f), Hill, Hand, 3, 0, MaterialCondition.NightOnly),
        // ── 날씨가 만드는 재료 (그 날씨에만 나타난다. 반딧불이는 밤에 날아다니는 것을 E로 잡는다 - FireflySwarm)
        new Entry("snow_flower",      "눈꽃",       Plant, "deco_snow_flower",     new Color(0.82f, 0.93f, 0.97f), Hill,  Hand, 6, 0, MaterialCondition.SnowDay),
        new Entry("mist_flower",      "안개꽃",     Plant, "deco_mist_flower",     new Color(0.96f, 0.96f, 0.96f), Hill,  Hand, 5, 0, MaterialCondition.FogDay),
        new Entry("dew_drop",         "이슬방울",   Deco,  "deco_dew_drop",        new Color(0.60f, 0.85f, 0.92f), Hill,  Hand, 4, 0, MaterialCondition.AfterRain),
        new Entry("sea_glass",        "바다 유리",  Deco,  "deco_sea_glass",       new Color(0.45f, 0.75f, 0.85f), Beach, Hand, 5, 0, MaterialCondition.AfterStorm),
        new Entry("firefly",          "반딧불이",   Deco,  "deco_firefly",         new Color(0.95f, 0.90f, 0.40f), MaterialZone.Anywhere, Hand, 8, 0, MaterialCondition.NightOnly),
        // ── 날씨가 만드는 재료 더 8종: 비 온 뒤 숲(이끼, 푸른 버섯, 청개구리), 안개 낀 날 숲(이슬 거미줄), 바람 부는 날 꽃 언덕(깃털, 홀씨), 폭풍우 치는 날 바닷가(번개 유리), 비 갠 날 바닷가(무지개 조약돌)
        new Entry("rain_moss",        "빗물 이끼",     Plant, "deco_rain_moss",       new Color(0.40f, 0.66f, 0.40f), Forest, Hand, 3, 0, MaterialCondition.AfterRain),
        new Entry("blue_mushroom",    "푸른 버섯",     Deco,  "deco_blue_mushroom",   new Color(0.44f, 0.67f, 0.85f), Forest, Hand, 5, 0, MaterialCondition.AfterRain),
        new Entry("tree_frog",        "청개구리",      Deco,  "deco_tree_frog",       new Color(0.45f, 0.70f, 0.42f), Forest, Hand, 7, 0, MaterialCondition.AfterRain),
        new Entry("spider_web",       "이슬 거미줄",   Deco,  "deco_spider_web",      new Color(0.95f, 0.95f, 0.92f), Forest, Hand, 5, 0, MaterialCondition.FogDay),
        new Entry("wind_feather",     "바람 깃털",     Deco,  "deco_wind_feather",    new Color(0.96f, 0.92f, 0.84f), Hill,   Hand, 4, 0, MaterialCondition.WindDay),
        new Entry("dandelion_puff",   "민들레 홀씨",   Plant, "deco_dandelion_puff",  new Color(0.96f, 0.95f, 0.88f), Hill,   Hand, 4, 0, MaterialCondition.WindDay),
        new Entry("lightning_glass",  "번개 유리",     Deco,  "deco_lightning_glass", new Color(0.80f, 0.92f, 0.95f), Beach,  Hand, 8, 0, MaterialCondition.StormOnly),
        new Entry("rainbow_pebble",   "무지개 조약돌", Deco,  "deco_rainbow_pebble",  new Color(0.70f, 0.60f, 0.88f), Beach,  Hand, 6, 0, MaterialCondition.RainbowDay),
        // ── 눈 섬(SnowIsland)에서만 나는 재료 11종: 겨울 소품. 소나무(도끼), 눈 더미(곡괭이), 바닥(줍기)
        new Entry("pine_sprig",       "솔잎 가지",       Deco,  "deco_pine_sprig",      new Color(0.28f, 0.50f, 0.35f), Snow, Axe,  2),
        new Entry("snowy_pinecone",   "눈 덮인 솔방울",  Deco,  "deco_snowy_pinecone",  new Color(0.85f, 0.85f, 0.88f), Snow, Axe,  3),
        new Entry("firewood",         "장작",            Deco,  "deco_firewood",        new Color(0.60f, 0.40f, 0.25f), Snow, Axe,  2),
        new Entry("ice_shard",        "얼음 조각",       Deco,  "deco_ice_shard",       new Color(0.65f, 0.88f, 0.95f), Snow, Pick, 4),
        new Entry("icicle",           "고드름",          Deco,  "deco_icicle",          new Color(0.75f, 0.92f, 0.97f), Snow, Pick, 3),
        new Entry("holly_berry",      "호랑가시 열매",   Plant, "deco_holly_berry",     new Color(0.85f, 0.25f, 0.25f), Snow, Hand, 3),
        new Entry("frost_leaf",       "서리 낀 잎",      Plant, "deco_frost_leaf",      new Color(0.70f, 0.88f, 0.85f), Snow, Hand, 2),
        new Entry("snowball",         "눈덩이",          Deco,  "deco_snowball",        new Color(0.95f, 0.97f, 1.00f), Snow, Hand, 1),
        new Entry("mini_snowman",     "미니 눈사람",     Deco,  "deco_mini_snowman",    new Color(0.95f, 0.97f, 1.00f), Snow, Hand, 8),       // 눈 섬의 눈사람을 곡괭이로 캐서 얻는다 (Harvestable.fixedMaterial)
        new Entry("winter_bell",      "겨울 방울",       Deco,  "deco_winter_bell",     new Color(0.93f, 0.75f, 0.25f), Snow, Hand, 6),
        new Entry("mitten",           "벙어리장갑",      Deco,  "deco_mitten",          new Color(0.85f, 0.25f, 0.25f), Shop, Buy, 5, 14),   // 눈 섬에서 줍지 않고 우체통에서 산다
        // ── 사막 섬(DesertIsland)에서만 나는 재료 14종: 선인장과 야자나무(도끼, 하나씩 정해져 있다), 바위(곡괭이), 바닥(줍기). 낙타, 야자나무, 천막, 피라미드는 우체통에서 산다
        new Entry("cactus_piece",     "선인장",          Plant, "deco_cactus_piece",    new Color(0.45f, 0.70f, 0.42f), Desert, Axe,  3),
        new Entry("date_fruit",       "대추야자",        Plant, "deco_date_fruit",      new Color(0.60f, 0.35f, 0.20f), Desert, Axe,  4),
        new Entry("sandstone",        "사암 조각",       Deco,  "deco_sandstone",       new Color(0.85f, 0.72f, 0.52f), Desert, Pick, 2),
        new Entry("desert_rose",      "사막 장미",       Deco,  "deco_desert_rose",     new Color(0.96f, 0.82f, 0.76f), Desert, Pick, 5),
        new Entry("fossil",           "화석",            Deco,  "deco_fossil",          new Color(0.76f, 0.65f, 0.48f), Desert, Pick, 20, 0, MaterialCondition.Always, true),
        new Entry("cactus_flower",    "선인장 꽃",       Plant, "deco_cactus_flower",   new Color(0.96f, 0.68f, 0.75f), Desert, Hand, 4),
        new Entry("aloe_leaf",        "알로에",          Plant, "deco_aloe_leaf",       new Color(0.45f, 0.70f, 0.42f), Desert, Hand, 3),
        new Entry("dry_branch",       "마른 가지",       Deco,  "deco_dry_branch",      new Color(0.76f, 0.65f, 0.48f), Desert, Hand, 2),
        new Entry("pottery_shard",    "옛 도자기 조각",  Deco,  "deco_pottery_shard",   new Color(0.81f, 0.40f, 0.36f), Desert, Hand, 6),
        new Entry("scarab",           "풍뎅이",          Deco,  "deco_scarab",          new Color(0.44f, 0.67f, 0.85f), Desert, Hand, 6),
        new Entry("camel",            "낙타",            Deco,  "deco_camel",           new Color(0.77f, 0.61f, 0.44f), Shop, Buy, 5, 26),
        new Entry("palm_tree",        "야자나무",        Deco,  "deco_palm_tree",       new Color(0.45f, 0.70f, 0.42f), Shop, Buy, 4, 18),
        new Entry("desert_tent",      "사막 천막",       Deco,  "deco_desert_tent",     new Color(0.85f, 0.35f, 0.33f), Shop, Buy, 4, 20),
        new Entry("pyramid",          "작은 피라미드",   Deco,  "deco_pyramid",         new Color(0.93f, 0.82f, 0.60f), Shop, Buy, 6, 30),        // ── 미쿠: 집 앞에 서 있는 NPC를 작게 줄인 꾸밈 재료 (MikuSetup이 모델을 만든다). 우체통 구매하기
        new Entry("miku",             "미쿠",            Deco,  "deco_miku",            new Color(0.22f, 0.78f, 0.78f), Shop, Buy, 6, 12),
        // ── 치비: 집 앞에 서 있는 NPC를 작게 줄인 꾸밈 재료 (ChibiSetup이 모델을 만든다). 우체통 구매하기
        new Entry("chibi",            "치비",            Deco,  "deco_chibi",           new Color(0.70f, 0.70f, 0.72f), Shop, Buy, 7, 14),
        // ── 섬의 풀과 꽃을 괭이로 캐면 무작위로 나오는 재료들 (따로 놓인 자리는 없다. 괭이로 풀은 풀 재료, 꽃은 꽃 재료가 나온다)
        new Entry("tulip",            "튤립",       Plant, "deco_tulip",           new Color(0.85f, 0.35f, 0.40f), Hill,  Hoe, 3),
        new Entry("sunflower",        "해바라기",   Plant, "deco_sunflower",       new Color(0.97f, 0.80f, 0.20f), Hill,  Hoe, 15, 0, MaterialCondition.Always, true),
        new Entry("lavender",         "라벤더",     Plant, "deco_lavender",        new Color(0.70f, 0.60f, 0.88f), Hill,  Hoe, 3),
        new Entry("cosmos",           "코스모스",   Plant, "deco_cosmos",          new Color(0.96f, 0.68f, 0.75f), Hill,  Hoe, 2),
        new Entry("dandelion",        "민들레",     Plant, "deco_dandelion",       new Color(0.98f, 0.85f, 0.30f), Hill,  Hoe, 2),
        new Entry("pink_flower",      "분홍 들꽃",  Plant, "deco_flower_pink",     new Color(0.96f, 0.68f, 0.75f), Hill,  Hoe, 2),
        new Entry("purple_flower",    "보라 들꽃",  Plant, "deco_flower_purple",   new Color(0.70f, 0.60f, 0.88f), Hill,  Hoe, 2),
        new Entry("wheat",            "밀 이삭",    Plant, "deco_wheat",           new Color(0.93f, 0.78f, 0.40f), MaterialZone.Anywhere, Hoe,  2),
        new Entry("four_leaf_clover", "네잎클로버", Plant, "deco_four_leaf_clover",new Color(0.45f, 0.78f, 0.45f), MaterialZone.Anywhere, Hoe, 15, 0, MaterialCondition.Always, true),
        new Entry("grass_tuft",       "풀 다발",    Plant, "deco_grass_tuft",      new Color(0.45f, 0.70f, 0.40f), MaterialZone.Anywhere, Hoe,  1),
        new Entry("reed",             "갈대",       Plant, "deco_reed",            new Color(0.85f, 0.75f, 0.55f), MaterialZone.Anywhere, Hoe,  2),
        new Entry("foxtail",          "강아지풀",   Plant, "deco_foxtail",         new Color(0.75f, 0.70f, 0.45f), MaterialZone.Anywhere, Hoe,  1),
        new Entry("green_leaf",       "나뭇잎",     Deco,  "deco_leaf_green",      new Color(0.40f, 0.70f, 0.40f), Forest, Hoe,  1),
        new Entry("orange_leaf",      "단풍잎",     Deco,  "deco_leaf_orange",     new Color(0.93f, 0.55f, 0.25f), Forest, Hoe,  2),
        new Entry("moss_dark",        "짙은 이끼",  MaterialKind.Cover, "deco_moss_dark", new Color(0.30f, 0.50f, 0.30f), Forest, Hoe,  1),
        new Entry("acorn",            "도토리",     Deco,  "deco_acorn",           new Color(0.60f, 0.40f, 0.25f), Forest, Hoe,  1),
        new Entry("brown_mushroom",   "갈색 버섯",  Deco,  "deco_mushroom_brown",  new Color(0.60f, 0.42f, 0.30f), Forest, Hoe,  2),
        new Entry("white_mushroom",   "흰 버섯",    Deco,  "deco_mushroom_white",  new Color(0.95f, 0.94f, 0.90f), Forest, Hoe,  2),

        // ── 희귀 재료 2종 (나무와 돌을 칠 때 가끔)
        new Entry("amber",            "호박 조각",  Deco,  "deco_amber",         new Color(0.93f, 0.65f, 0.20f), MaterialZone.Anywhere, Axe,  20, 0, MaterialCondition.Always, true),
        new Entry("crystal",          "수정 조각",  Deco,  "deco_crystal",       new Color(0.75f, 0.67f, 0.95f), MaterialZone.Anywhere, Pick, 20, 0, MaterialCondition.Always, true),

        // ── 우체통 상점에서 사는 소품 16종 (자연에서는 못 구한다)
        new Entry("book",             "책",         Deco,  "deco_book",            new Color(0.45f, 0.62f, 0.90f), Shop, Buy, 2, 10),
        new Entry("book_stack",       "책 더미",    Deco,  "deco_book_stack",      new Color(0.45f, 0.70f, 0.50f), Shop, Buy, 3, 18),
        new Entry("umbrella",         "우산",       Deco,  "deco_umbrella",        new Color(0.85f, 0.35f, 0.33f), Shop, Buy, 2, 15),
        new Entry("campfire",         "캠프파이어", Deco,  "deco_campfire",        new Color(0.60f, 0.40f, 0.25f), Shop, Buy, 3, 20),
        new Entry("tent",             "텐트",       Deco,  "deco_tent",            new Color(0.45f, 0.62f, 0.90f), Shop, Buy, 3, 25),
        new Entry("signpost",         "이정표",     Deco,  "deco_signpost",        new Color(0.69f, 0.51f, 0.35f), Shop, Buy, 2, 15),
        new Entry("paper_boat",       "종이배",     Deco,  "deco_paper_boat",      new Color(0.96f, 0.68f, 0.75f), Shop, Buy, 1, 8),
        new Entry("candle",           "초",         Deco,  "deco_candle",          new Color(0.96f, 0.95f, 0.92f), Shop, Buy, 1, 6),
        new Entry("teacup",           "찻잔",       Deco,  "deco_teacup",          new Color(0.96f, 0.95f, 0.92f), Shop, Buy, 2, 10),
        new Entry("wind_chime",       "바람종",     Deco,  "deco_wind_chime",      new Color(0.60f, 0.78f, 0.85f), Shop, Buy, 3, 20),
        new Entry("star_ornament",    "별 장식",    Deco,  "deco_star_ornament",   new Color(0.97f, 0.85f, 0.40f), Shop, Buy, 2, 12),
        new Entry("crescent_moon",    "초승달",     Deco,  "deco_crescent_moon",   new Color(0.97f, 0.85f, 0.40f), Shop, Buy, 2, 12),
        new Entry("cairn",            "돌탑",       Deco,  "deco_cairn",           new Color(0.62f, 0.62f, 0.65f), Shop, Buy, 2, 14),
        new Entry("ladder",           "사다리",     Deco,  "deco_ladder",          new Color(0.69f, 0.51f, 0.35f), Shop, Buy, 2, 12),
        new Entry("fairy_door",       "요정 문",    Deco,  "deco_fairy_door",      new Color(0.62f, 0.62f, 0.65f), Shop, Buy, 3, 30),
        new Entry("hot_air_balloon",  "열기구",     Deco,  "deco_hot_air_balloon", new Color(0.85f, 0.35f, 0.33f), Shop, Buy, 3, 40),
        // ── 얼어붙은 연못 낚시(FishingSpot)로 얻는 물고기. 바닥에 놓여 있지 않다. 오로라 물고기는 밤에만 가끔 걸린다
        new Entry("smelt",            "빙어",          Deco, "deco_smelt",           new Color(0.80f, 0.85f, 0.92f), Snow, Hand, 3),
        new Entry("ice_trout",        "얼음 송어",     Deco, "deco_ice_trout",       new Color(0.45f, 0.70f, 0.85f), Snow, Hand, 5),
        new Entry("goldfish",         "금붕어",        Deco, "deco_goldfish",        new Color(0.95f, 0.60f, 0.32f), Snow, Hand, 6),
        new Entry("aurora_fish",      "오로라 물고기", Deco, "deco_aurora_fish",     new Color(0.70f, 0.58f, 0.90f), Snow, Hand, 20, 0, MaterialCondition.Always, true),        // ── 꾸밈 소품 11종, 동물 4종, 병 속 효과 5종 (우체통 구매하기). 효과 병은 병에 놓으면 병 속에 입자가 흩날린다 (BottleEffect, id가 fx_로 시작)
        new Entry("lantern",          "작은 랜턴",   Deco, "deco_lantern",         new Color(1.00f, 0.80f, 0.45f), Shop, Buy, 2, 10),
        new Entry("flag",             "깃발",        Deco, "deco_flag",            new Color(0.85f, 0.35f, 0.33f), Shop, Buy, 1, 6),
        new Entry("kite",             "연",          Deco, "deco_kite",            new Color(0.96f, 0.68f, 0.75f), Shop, Buy, 2, 8),
        new Entry("fence",            "나무 울타리", Deco, "deco_fence",           new Color(0.77f, 0.61f, 0.44f), Shop, Buy, 2, 8),
        new Entry("bench",            "벤치",        Deco, "deco_bench",           new Color(0.77f, 0.61f, 0.44f), Shop, Buy, 3, 14),
        new Entry("telescope",        "망원경",      Deco, "deco_telescope",       new Color(0.45f, 0.68f, 0.85f), Shop, Buy, 3, 16),
        new Entry("sailboat",         "돛단배",      Deco, "deco_sailboat",        new Color(0.60f, 0.40f, 0.25f), Shop, Buy, 3, 16),
        new Entry("stone_bridge",     "돌다리",      Deco, "deco_stone_bridge",    new Color(0.62f, 0.62f, 0.66f), Shop, Buy, 4, 18),
        new Entry("fountain",         "분수",        Deco, "deco_fountain",        new Color(0.60f, 0.85f, 0.92f), Shop, Buy, 5, 26),
        new Entry("lighthouse",       "작은 등대",   Deco, "deco_lighthouse",      new Color(0.85f, 0.35f, 0.33f), Shop, Buy, 5, 28),
        new Entry("mushroom_house",   "버섯 집",     Deco, "deco_mushroom_house",  new Color(0.85f, 0.35f, 0.33f), Shop, Buy, 5, 30),
        new Entry("bird",             "작은 새",     Deco, "deco_bird",            new Color(0.49f, 0.66f, 0.89f), Shop, Buy, 4, 20),
        new Entry("rabbit",           "토끼",        Deco, "deco_rabbit",          new Color(0.96f, 0.95f, 0.92f), Shop, Buy, 5, 24),
        new Entry("cat",              "고양이",      Deco, "deco_cat",             new Color(0.94f, 0.56f, 0.40f), Shop, Buy, 5, 24),
        new Entry("fox",              "여우",        Deco, "deco_fox",             new Color(0.95f, 0.60f, 0.32f), Shop, Buy, 5, 26),
        new Entry("fx_bubbles",       "비눗방울",    Deco, "deco_fx_bubbles",      new Color(0.60f, 0.85f, 0.95f), Shop, Buy, 3, 12),
        new Entry("fx_rain",          "보슬비",      Deco, "deco_fx_rain",         new Color(0.49f, 0.66f, 0.89f), Shop, Buy, 3, 14),
        new Entry("fx_glitter",       "반짝이 가루", Deco, "deco_fx_glitter",      new Color(0.93f, 0.75f, 0.25f), Shop, Buy, 4, 15),
        new Entry("fx_petals",        "꽃잎 날리기", Deco, "deco_fx_petals",       new Color(0.96f, 0.68f, 0.75f), Shop, Buy, 4, 15),
        new Entry("fx_snow",          "눈 내리기",   Deco, "deco_fx_snow",         new Color(0.80f, 0.92f, 0.97f), Shop, Buy, 5, 18),        // 눈 에셋의 건물을 작게 줄인 꾸밈 재료 (BuildingMaterialSetup이 모델을 만든다)
        new Entry("snow_cottage1",    "눈 오두막 1", Deco, "deco_snow_cottage1",   new Color(0.92f, 0.75f, 0.55f), Shop, Buy, 5, 30),
        new Entry("snow_cottage2",    "눈 오두막 2", Deco, "deco_snow_cottage2",   new Color(0.92f, 0.75f, 0.55f), Shop, Buy, 5, 32),
        new Entry("snow_cottage3",    "눈 오두막 3", Deco, "deco_snow_cottage3",   new Color(0.92f, 0.75f, 0.55f), Shop, Buy, 5, 35),
        new Entry("snow_well",        "눈 우물",     Deco, "deco_snow_well",       new Color(0.80f, 0.72f, 0.60f), Shop, Buy, 4, 24),
    };

    // 괭이로 캘 때 꽃에서 나오는 재료 (나머지 식물, 이끼, 잎, 열매, 버섯은 풀에서 나온다)
    private static readonly System.Collections.Generic.HashSet<string> FlowerIds = new System.Collections.Generic.HashSet<string>
    {
        "white_flower", "blue_flower", "evening_primrose", "tulip", "sunflower", "lavender", "cosmos", "dandelion", "pink_flower", "purple_flower",
        "snow_flower", "mist_flower"
    };
    // 풀에서 나오는 재료. 괭이 재료(Hoe)는 모두 풀이거나 꽃이고, 맨손으로 줍던 식물 몇 가지도 풀에서 나온다.
    private static readonly System.Collections.Generic.HashSet<string> GrassHandIds = new System.Collections.Generic.HashSet<string>
    {
        "moss", "fern", "clover"
    };

    private static PlantGroup GroupOf(string id)
    {
        if (FlowerIds.Contains(id)) return PlantGroup.Flower;
        foreach (Entry e in Entries)
            if (e.id == id && (e.acquire == AcquireMethod.Hoe || GrassHandIds.Contains(id))) return PlantGroup.Grass;
        return PlantGroup.None;
    }

    [MenuItem("유리병 정원/재료 데이터 만들기")]
    public static string Build()
    {
        int created = 0, missingModel = 0;
        foreach (Entry e in Entries)
        {
            string path = DataFolder + "/" + e.id + ".asset";
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MaterialData>();
                AssetDatabase.CreateAsset(data, path);
                created++;
            }

            data.id = e.id;
            data.displayName = e.displayName;
            data.kind = e.kind;
            data.placeholderColor = e.color;
            data.placedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + e.model + ".obj");
            if (data.placedPrefab == null) data.placedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + e.model + ".prefab"); // 눈 에셋 건물은 줄여 놓은 프리팹이다
            if (data.placedPrefab == null) missingModel++;
            data.zone = e.zone; data.acquire = e.acquire; data.condition = e.condition; data.isRare = e.rare;
            data.plantGroup = GroupOf(e.id);
            data.sellPrice = e.sell; data.buyPrice = e.buy;
            EditorUtility.SetDirty(data);
        }

        // 바닥재 모래: 바닷가에서 맨손으로 줍는다
        MaterialData sand = BottleSetup.EnsureSandAsset();
        sand.zone = Beach; sand.acquire = Hand; sand.condition = MaterialCondition.Always; sand.sellPrice = 1;
        EditorUtility.SetDirty(sand);

        // 바닥재 눈: 눈 섬에서 맨손으로 줍는다 (모래와 같은 방식)
        MaterialData snow = BottleSetup.EnsureSnowAsset();
        snow.zone = Snow; snow.acquire = Hand; snow.condition = MaterialCondition.Always; snow.sellPrice = 1;
        EditorUtility.SetDirty(snow);

        AssetDatabase.SaveAssets();
        return "재료 데이터 " + (Entries.Length + 2) + "종 (새로 " + created + "개), 모델 못 찾음 " + missingModel + "개";
    }
}
