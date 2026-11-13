using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>병 안에 놓인 재료 하나의 기록 (저장할 내용)</summary>
public class PlacedData
{
    public MaterialData material;
    public Vector3 localPosition; // 병 기준 위치
    public float yaw;
    public float scaleFactor;
}

/// <summary>병 하나의 기록 (저장할 내용)</summary>
public class BottleRecord
{
    public List<PlacedData> items = new List<PlacedData>();
    public MaterialData ground;   // 바닥재. null이면 흙
    public bool completed;        // 완성해서 닫았는가
    public bool created;          // 병을 만들었는가 (만들 때 병 종류와 흙의 양을 정하고, 그 뒤로는 바꿀 수 없다)
    public int soilLevel = 1;     // 흙의 양: 0 적게, 1 보통, 2 많이
    public int shelfSlot = -1;    // 선반의 몇 번째 칸에 놓였는가 (-1이면 선반에 없다)
}

/// <summary>
/// 병들의 상태를 메모리에 기억해 둔다. 집 밖으로 나가 씬이 바뀌어도 병 속이 그대로 돌아온다.
/// 파일에 저장하는 것은 SaveSystem이 이 기록을 읽고 쓰는 방식이다.
/// </summary>
public static class BottleStore
{
    private static readonly Dictionary<string, BottleRecord> bottles = new Dictionary<string, BottleRecord>();

    /// <summary>병의 기록이 바뀔 때(재료를 놓음, 완성함 등) 알린다. 선반이 완성한 병을 올릴 때 쓴다.</summary>
    public static event Action Changed;

    /// <summary>모든 병의 기록 (저장할 때 쓴다)</summary>
    public static IEnumerable<KeyValuePair<string, BottleRecord>> All => bottles;

    /// <summary>병의 기록을 돌려준다. 처음 보는 병이면 빈 기록을 만든다.</summary>
    public static BottleRecord Get(string bottleId)
    {
        if (!bottles.TryGetValue(bottleId, out BottleRecord record))
        {
            record = new BottleRecord();
            bottles[bottleId] = record;
        }
        return record;
    }

    public static void Set(string bottleId, BottleRecord record)
    {
        // 이미 선반에 있는 병은 새 기록으로 덮어쓰면서 칸 번호가 지워지지 않게 이어 받는다
        if (bottles.TryGetValue(bottleId, out BottleRecord old) && record.shelfSlot < 0)
            record.shelfSlot = old.shelfSlot;
        bottles[bottleId] = record;
        Changed?.Invoke();
    }

    /// <summary>완성한 병의 수 (바닷가와 꽃 언덕을 여는 조건에 쓴다)</summary>
    public static int CompletedCount
    {
        get
        {
            int count = 0;
            foreach (BottleRecord record in bottles.Values)
                if (record.completed) count++;
            return count;
        }
    }

    /// <summary>선반의 칸 수. 처음에는 6칸이고, 완성한 병이 6개, 12개, 18개가 될 때 6칸씩 늘어 최대 24칸이다. (기획서 6.3)</summary>
    public static int ShelfCapacity => Mathf.Min(24, 6 + (CompletedCount / 6) * 6);

    /// <summary>이 병이 지금 선반에 전시되어 있는가</summary>
    public static bool IsDisplayed(string bottleId)
    {
        BottleRecord record = Get(bottleId);
        return record.completed && record.shelfSlot >= 0;
    }

    /// <summary>완성한 병을 선반의 가장 앞 빈 칸에 전시한다. 완성하지 않았거나 선반이 가득 차면 false.</summary>
    public static bool TryDisplay(string bottleId)
    {
        BottleRecord record = Get(bottleId);
        if (!record.completed) return false;
        if (record.shelfSlot >= 0) return true; // 이미 전시 중
        int slot = FirstFreeShelfSlot();
        if (slot < 0) return false;
        record.shelfSlot = slot;
        Changed?.Invoke();
        return true;
    }

    /// <summary>선반에서 병을 내린다. (병은 그대로 남고, 선반 칸만 비워진다)</summary>
    public static void Undisplay(string bottleId)
    {
        Get(bottleId).shelfSlot = -1;
        Changed?.Invoke();
    }

    /// <summary>선반의 비어 있는 가장 앞 칸 번호. 모두 차 있으면 -1.</summary>
    public static int FirstFreeShelfSlot()
    {
        var used = new HashSet<int>();
        foreach (BottleRecord record in bottles.Values)
            if (record.completed && record.shelfSlot >= 0) used.Add(record.shelfSlot);
        for (int i = 0; i < ShelfCapacity; i++)
            if (!used.Contains(i)) return i;
        return -1;
    }

    // Play를 껐다 켤 때 기록이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        bottles.Clear();
        Changed = null;
    }
}
