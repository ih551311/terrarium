using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도끼와 곡괭이의 단계별 수치와, 캐서 나오는 재료 목록. (기획서 6.8) 모두 임시 값이라 에셋에서 고치면 된다.
/// 에셋은 메뉴 "유리병 정원 > 도구 설정 만들기"가 Assets/Resources/ToolSettings.asset 으로 만든다.
/// </summary>
public class ToolSettings : ScriptableObject
{
    [System.Serializable]
    public struct Level
    {
        [Tooltip("재료가 나오는 데 필요한 타격 수")]
        public int hits;
        [Tooltip("한 번에 얻는 양")]
        public int amount;
        [Tooltip("희귀 재료가 나올 확률 (0~1)")]
        [Range(0f, 1f)] public float rareChance;
        [Tooltip("이 단계로 강화하는 데 드는 코인 (우체통 주문에서 쓴다)")]
        public int upgradeCost;
    }

    [Tooltip("도끼의 0단계(기본)부터 3단계(최대)까지")]
    public Level[] axeLevels;
    [Tooltip("곡괭이의 0단계(기본)부터 3단계(최대)까지")]
    public Level[] pickaxeLevels;
    [Tooltip("괭이. 강화는 없어서 첫 칸만 쓴다. 비워 두면 기본값(2번 치기, 1개)")]
    public Level[] hoeLevels;
    [Tooltip("도끼나 곡괭이로 캘 때 나올 수 있는 모든 재료 (희귀 재료 포함)")]
    public MaterialData[] harvestMaterials;

    private static ToolSettings instance;

    public static ToolSettings Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<ToolSettings>("ToolSettings");
            return instance;
        }
    }

    /// <summary>도구(Axe 또는 Pickaxe)의 단계별 수치. 단계가 범위를 벗어나면 가장 가까운 단계를 쓴다.</summary>
    public Level Get(AcquireMethod tool, int level)
    {
        if (tool == AcquireMethod.Hoe)
        {
            if (hoeLevels == null || hoeLevels.Length == 0) return new Level { hits = 2, amount = 1, rareChance = 0f, upgradeCost = 0 };
            return hoeLevels[Mathf.Clamp(level, 0, hoeLevels.Length - 1)];
        }
        Level[] levels = tool == AcquireMethod.Axe ? axeLevels : pickaxeLevels;
        return levels[Mathf.Clamp(level, 0, levels.Length - 1)];
    }

    /// <summary>
    /// 괭이로 캘 때 나올 재료 하나를 고른다. group(풀 또는 꽃)에 맞는 재료 중에서, 이 구역의 것을 먼저 고른다.
    /// 후보: 얻는 방법이 Hoe인 재료와, 맨손으로 줍는 식물 중 갈래가 정해진 것. (밤에만, 비 온 뒤에만 나는 것은 뺀다)
    /// zone이 Anywhere(가운데 풀밭)면 모든 구역의 재료에서 고른다.
    /// </summary>
    public MaterialData RollHoe(PlantGroup group, MaterialZone zone)
    {
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db == null || db.materials == null) return null;

        var all = new List<MaterialData>();
        var local = new List<MaterialData>();
        foreach (MaterialData m in db.materials)
        {
            if (m == null || m.isRare || m.plantGroup != group || m.condition != MaterialCondition.Always) continue;
            bool hoe = m.acquire == AcquireMethod.Hoe;
            bool plantLike = m.acquire == AcquireMethod.Hand && (m.kind == MaterialKind.Plant || m.kind == MaterialKind.Cover);
            if (!hoe && !plantLike) continue;
            all.Add(m);
            if (m.zone == zone || m.zone == MaterialZone.Anywhere) local.Add(m);
        }
        List<MaterialData> pool = zone != MaterialZone.Anywhere && local.Count > 0 ? local : all;
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    /// <summary>
    /// 캘 때 나올 재료 하나를 고른다. 희귀 확률에 당첨되면 이 도구의 희귀 재료를, 아니면 이 구역의 보통 재료를 고른다.
    /// 이 구역에 맞는 보통 재료가 없으면(예: 꽃 언덕의 나무) 이 도구의 보통 재료 전체에서 고른다.
    /// </summary>
    public MaterialData Roll(AcquireMethod tool, MaterialZone zone, float rareChance, out bool rare)
    {
        var normal = new List<MaterialData>();
        var zoneNormal = new List<MaterialData>();
        var rares = new List<MaterialData>();
        foreach (MaterialData m in harvestMaterials)
        {
            if (m == null || m.acquire != tool) continue;
            if (m.isRare) rares.Add(m);
            else
            {
                normal.Add(m);
                if (m.zone == zone) zoneNormal.Add(m);
            }
        }

        rare = rares.Count > 0 && Random.value < rareChance;
        if (rare) return rares[Random.Range(0, rares.Count)];

        List<MaterialData> pool = zoneNormal.Count > 0 ? zoneNormal : normal;
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }
}
