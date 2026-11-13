using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체의 "오늘"과 지갑, 우체통 안의 것들을 기억한다. (씬을 옮겨도 남는다. 디스크 저장은 5단계에서 붙인다)
/// - 날짜: 침대에서 자면 하루가 지난다.
/// - 코인: 우체통에 재료를 보내면 다음 날 아침에 코인이 오고, 주문한 물건도 다음 날 아침에 온다.
/// 우체통 패널(MailboxPanel)이 이 값을 읽고 바꾼다.
/// </summary>
public static class GameState
{
    private static readonly List<MaterialData> outbox = new List<MaterialData>();       // 보낸 재료 (자고 나면 코인으로)
    private static readonly List<MaterialData> orders = new List<MaterialData>();       // 주문한 물건 (자고 나면 도착)
    private static readonly List<MaterialData> arrivedItems = new List<MaterialData>(); // 도착해서 우체통에 있는 물건

    // 오늘 이미 주웠거나 캔 자리 (다음 날 아침에 비워진다). 집에 들어갔다 나와도 그대로 남아 있게 여기에 적어 둔다.
    private static readonly HashSet<string> taken = new HashSet<string>();

    /// <summary>1일째부터 시작한다.</summary>
    public static int Day { get; private set; } = 1;

    /// <summary>하루의 길이(초). 실제 시간으로 약 10분이다. (기획서 6.4)</summary>
    public const float DayLengthSeconds = 600f;

    /// <summary>하루 중의 시간. 0이 아침, 1이 밤 끝이다. 밤에서 멈추고, 침대에서 자야 다음 날 아침(0)이 된다.</summary>
    public static float TimeOfDay { get; private set; }

    /// <summary>시간을 흘려보낸다. (GameRoot가 매 프레임 부른다) 밤 끝(1)에서 멈춘다.</summary>
    public static void Tick(float deltaSeconds)
    {
        TimeOfDay = Mathf.Min(1f, TimeOfDay + deltaSeconds / DayLengthSeconds);
    }

    /// <summary>시간을 직접 정한다. (시험용)</summary>
    public static void SetTimeOfDay(float t)
    {
        TimeOfDay = Mathf.Clamp01(t);
    }

    /// <summary>도끼의 강화 단계 (0이 기본, 3이 최대). 강화 주문은 13단계에서 붙인다.</summary>
    public static int AxeLevel { get; private set; }

    /// <summary>곡괭이의 강화 단계 (0이 기본, 3이 최대)</summary>
    public static int PickaxeLevel { get; private set; }

    /// <summary>도구(Axe 또는 Pickaxe)의 강화 단계</summary>
    public static int LevelOf(AcquireMethod tool)
    {
        return tool == AcquireMethod.Axe ? AxeLevel : PickaxeLevel;
    }

    /// <summary>아침이 되어 주운 자리와 캔 자리가 다시 채워질 때 호출된다.</summary>
    public static event Action DayStarted;

    /// <summary>오늘 이 자리(id)의 재료를 가져갔다고 적는다.</summary>
    public static void MarkTaken(string id)
    {
        taken.Add(id);
    }

    /// <summary>오늘 이 자리(id)의 재료를 이미 가져갔는가</summary>
    public static bool IsTaken(string id)
    {
        return taken.Contains(id);
    }

    /// <summary>지갑의 코인</summary>
    public static int Coins { get; private set; }

    /// <summary>도착해서 우체통에서 받기를 기다리는 코인</summary>
    public static int ArrivedCoins { get; private set; }

    public static IReadOnlyList<MaterialData> Outbox => outbox;
    public static IReadOnlyList<MaterialData> Orders => orders;
    public static IReadOnlyList<MaterialData> ArrivedItems => arrivedItems;

    /// <summary>도착해서 받을 것이 있는가</summary>
    public static bool HasMail => ArrivedCoins > 0 || arrivedItems.Count > 0;

    /// <summary>보낸 재료가 내일 아침 가져다줄 코인</summary>
    public static int PendingSellCoins
    {
        get
        {
            int total = 0;
            foreach (MaterialData m in outbox) total += m.sellPrice;
            return total;
        }
    }

    /// <summary>값이 바뀔 때마다 호출된다. (화면 갱신용)</summary>
    public static event Action Changed;

    /// <summary>재료 1개를 우체통에 넣는다. (가방에서 빼는 일은 부르는 쪽이 한다)</summary>
    public static void Send(MaterialData material)
    {
        if (material == null) return;
        outbox.Add(material);
        Changed?.Invoke();
    }

    /// <summary>코인을 내고 물건을 주문한다. 코인이 모자라거나 팔지 않는 물건이면 false.</summary>
    public static bool TryOrder(MaterialData material)
    {
        if (material == null || material.buyPrice <= 0 || Coins < material.buyPrice) return false;
        Coins -= material.buyPrice;
        orders.Add(material);
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// 자고 일어나 다음 날이 된다. 보낸 재료는 코인으로, 주문한 물건은 도착한 물건으로 바뀐다.
    /// 새로 도착한 것이 있으면 true.
    /// </summary>
    public static bool Sleep()
    {
        bool anything = outbox.Count > 0 || orders.Count > 0;

        ArrivedCoins += PendingSellCoins;
        arrivedItems.AddRange(orders);
        outbox.Clear();
        orders.Clear();
        Day++;

        taken.Clear(); // 주운 자리와 캔 자리가 다시 채워진다
        TimeOfDay = 0f; // 아침이 된다
        DayStarted?.Invoke();
        Changed?.Invoke();
        return anything;
    }

    /// <summary>
    /// 도착한 코인과 물건을 받는다. 코인은 지갑으로, 물건은 가방으로 들어간다.
    /// 가방이 가득 차면 못 넣은 물건은 우체통에 남는다.
    /// </summary>
    public static void Collect(Bag bag, out int coins, out int items, out int left)
    {
        coins = ArrivedCoins;
        Coins += ArrivedCoins;
        ArrivedCoins = 0;

        items = 0;
        for (int i = 0; i < arrivedItems.Count; )
        {
            if (bag != null && bag.TryAdd(arrivedItems[i]))
            {
                arrivedItems.RemoveAt(i);
                items++;
            }
            else
            {
                i++;
            }
        }
        left = arrivedItems.Count;

        Changed?.Invoke();
    }

    // ───────── 저장과 불러오기 ─────────

    /// <summary>지금 상태를 저장 내용(SaveData)에 적는다.</summary>
    public static void Export(SaveData data)
    {
        data.day = Day;
        data.coins = Coins;
        data.arrivedCoins = ArrivedCoins;
        data.axeLevel = AxeLevel;
        data.pickaxeLevel = PickaxeLevel;
        foreach (MaterialData m in outbox) data.outbox.Add(m.id);
        foreach (MaterialData m in orders) data.orders.Add(m.id);
        foreach (MaterialData m in arrivedItems) data.arrivedItems.Add(m.id);
    }

    /// <summary>저장 내용으로 상태를 되살린다. (게임을 켤 때 한 번)</summary>
    public static void Import(SaveData data, MaterialDatabase db)
    {
        Day = Mathf.Max(1, data.day);
        Coins = data.coins;
        ArrivedCoins = data.arrivedCoins;
        AxeLevel = data.axeLevel;
        PickaxeLevel = data.pickaxeLevel;

        outbox.Clear(); orders.Clear(); arrivedItems.Clear(); taken.Clear();
        foreach (string id in data.outbox) { MaterialData m = db.Find(id); if (m != null) outbox.Add(m); }
        foreach (string id in data.orders) { MaterialData m = db.Find(id); if (m != null) orders.Add(m); }
        foreach (string id in data.arrivedItems) { MaterialData m = db.Find(id); if (m != null) arrivedItems.Add(m); }
    }

    // Play를 껐다 켤 때 값이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        outbox.Clear();
        orders.Clear();
        arrivedItems.Clear();
        taken.Clear();
        TimeOfDay = 0f;
        AxeLevel = 0;
        PickaxeLevel = 0;
        DayStarted = null;
        Day = 1;
        Coins = 0;
        ArrivedCoins = 0;
        Changed = null;
    }
}
