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
}

/// <summary>
/// 병들의 상태를 메모리에 기억해 둔다. 집 밖으로 나가 씬이 바뀌어도 병 속이 그대로 돌아온다.
/// 파일에 저장하는 것은 5단계에서 이 기록을 읽고 쓰는 방식으로 붙인다.
/// </summary>
public static class BottleStore
{
    private static readonly Dictionary<string, BottleRecord> bottles = new Dictionary<string, BottleRecord>();

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
        bottles[bottleId] = record;
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

    // Play를 껐다 켤 때 기록이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        bottles.Clear();
    }
}
