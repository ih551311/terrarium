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

/// <summary>
/// 병 속 재료를 메모리에 기억해 둔다. 집 밖으로 나가 씬이 바뀌어도 병 속이 그대로 돌아온다.
/// 파일에 저장하는 것은 5단계에서 이 기록을 읽고 쓰는 방식으로 붙인다.
/// </summary>
public static class BottleStore
{
    private static readonly Dictionary<string, List<PlacedData>> bottles = new Dictionary<string, List<PlacedData>>();

    public static List<PlacedData> Get(string bottleId)
    {
        return bottles.TryGetValue(bottleId, out List<PlacedData> list) ? list : new List<PlacedData>();
    }

    public static void Set(string bottleId, List<PlacedData> list)
    {
        bottles[bottleId] = list;
    }

    // Play를 껐다 켤 때 기록이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        bottles.Clear();
    }
}
