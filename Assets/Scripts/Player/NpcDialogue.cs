using System.Collections.Generic;

/// <summary>대화하는 NPC의 성격 (NpcTalk가 고른다)</summary>
public enum NpcPersona { Miku }

/// <summary>
/// NPC 대사 모음. 날씨, 시간, 우체통과 병 만들기 진행 상황에 맞는 대사를 섞어서 말을 걸 때마다 다른 말이 나오게 한다.
/// 같은 대사가 바로 다시 나오지 않도록 NpcTalk가 최근에 한 말을 기억해서 넘겨 준다.
/// 미쿠는 밝고 수다스러운 말투(반말)로 말한다. (치비는 말하지 않는다)
/// (글꼴에 없는 글자 · → 는 쓰지 않는다)
/// </summary>
public static class NpcDialogue
{
    public static string Pick(NpcPersona persona, List<string> recent)
    {
        var contextual = new List<string>();
        var generic = new List<string>();
        MikuLines(contextual, generic);

        // 상황에 맞는 말을 더 자주 하되, 같은 말이 연달아 나오지 않게 한다
        var pool = new List<string>();
        foreach (string s in contextual) { if (!recent.Contains(s)) { pool.Add(s); pool.Add(s); } }
        foreach (string s in generic) if (!recent.Contains(s)) pool.Add(s);
        if (pool.Count == 0) { pool.AddRange(contextual); pool.AddRange(generic); }
        if (pool.Count == 0) return "...";

        string pick = pool[UnityEngine.Random.Range(0, pool.Count)];
        recent.Add(pick);
        while (recent.Count > 6) recent.RemoveAt(0);
        return pick;
    }

    private static bool IsNight => GameState.TimeOfDay >= Pickup.NightStart;
    private static bool IsMorning => GameState.TimeOfDay < 0.15f;

    private static void MikuLines(List<string> c, List<string> g)
    {
        // ── 날씨
        switch (Weather.Today)
        {
            case WeatherKind.Clear:
                c.Add("오늘 날씨 진짜 좋다. 풀 캐러 가기 딱이야");
                c.Add("햇볕이 따뜻해서 자꾸 졸려. 눈 감으면 안 되는데");
                break;
            case WeatherKind.Cloudy:
                c.Add("구름이 잔뜩이네. 비 올 것 같은데 우산 챙겼어?");
                break;
            case WeatherKind.Windy:
                c.Add("바람이 세서 머리가 엉망이야. 바람 깃털이 날아다닌다던데 봤어?");
                c.Add("바람 부는 날엔 민들레 홀씨가 날아다닌대. 잡으러 가 보자");
                break;
            case WeatherKind.Drizzle:
                c.Add("이슬비가 오네. 이런 날 숲에는 버섯이 난대");
                break;
            case WeatherKind.Rain:
                c.Add("비 온다! 숲에 버섯 났을지도 몰라. 개구리도 나왔으려나");
                c.Add("빗소리 들으면서 가만히 있는 거 좋아해. 너도 해 봐");
                break;
            case WeatherKind.Storm:
                c.Add("천둥 소리 엄청나다. 나는 무섭지 않아. 진짜야. 조금만 무서워");
                c.Add("폭풍우가 지나가면 바닷가에 바다 유리가 밀려온대. 같이 가자");
                break;
            case WeatherKind.Fog:
                c.Add("안개가 자욱해. 길 잃지 않게 조심해. 나도 아까 뱅뱅 돌았어");
                break;
            case WeatherKind.Snow:
                c.Add("눈이 와! 병 속에도 눈 내리게 할 수 있다던데 알아?");
                break;
        }
        if (Weather.RainbowDay) c.Add("어제 비 왔으니까 바닷가에 무지개 조약돌이 있을지도 몰라");

        // ── 시간
        if (IsNight)
        {
            c.Add("밤이야. 별이 엄청 많아. 병 속에 별 넣으면 예쁘겠다");
            c.Add("슬슬 자야 하는 거 아니야? 침대에서 자면 아침이 된대");
        }
        else if (IsMorning)
        {
            c.Add("좋은 아침! 일찍 일어났네. 오늘은 뭐 할 거야?");
        }

        // ── 진행 상황
        if (GameState.HasUnreadRequest) c.Add("우체통에 편지 와 있던데 봤어? 누가 병을 부탁하는 것 같았어");
        else if (GameState.RequestId >= 0) c.Add("편지 부탁은 어떻게 돼 가? 천천히 해도 된대");
        if (GameState.HasMail) c.Add("우체통에 뭔가 도착한 것 같더라. 받아 가");
        if (BottleStore.CompletedCount == 0) c.Add("병 하나 만들어 봤어? 집 안 작업대에서 만들 수 있어");
        else if (BottleStore.CompletedCount >= 12) c.Add("병이 벌써 이만큼이나 생겼네. 선반이 꽉 차겠다");
        else if (BottleStore.CompletedCount >= 4) c.Add("네 병 구경해도 돼? 선반에 올려 둔 거 봤는데 예쁘더라");
        if (!GameState.DockOpen && GameState.Coins >= 150) c.Add("코인이 꽤 모였네. 선착장을 열면 눈 섬에 갈 수 있대");
        else if (GameState.DockOpen && !GameState.DesertDockOpen) c.Add("눈 섬은 어땠어? 눈사람이 진짜 많지. 하나 캐 와도 돼?");
        else if (GameState.DesertDockOpen) c.Add("사막 섬도 가 봤구나. 선인장은 만지면 아파. 나도 만져 봤어");
        if (GameState.Day <= 2) c.Add("여기 처음 왔지? 천천히 둘러봐. 서두를 거 없어");

        // ── 아무 때나 하는 말
        g.Add("안녕 나는 미쿠야");
        g.Add("이 섬은 조용해서 좋아. 너도 그렇지?");
        g.Add("뭐든 열심히 하는구나. 쉬엄쉬엄 해도 돼");
        g.Add("해 질 때 노을 보는 게 제일 좋아. 오늘도 볼 거야?");
        g.Add("도토리 줍다가 다람쥐한테 혼났어. 자기 거래");
        g.Add("집 문 열어 봤어? 안이 생각보다 넓어. 나도 놀랐어");
        g.Add("풀이랑 꽃은 괭이, 나무는 도끼, 돌은 곡괭이야. 알지?");
        g.Add("병 속에 뭐 넣을지 고민될 땐 일단 하나만 놓아 봐");
        g.Add("발소리 들려? 사박사박. 나는 이 소리가 좋아");
        g.Add("나 오늘 아무것도 안 했어. 근데 그게 좋은 하루야");
    }
}
