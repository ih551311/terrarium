using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 재료(MaterialData) 목록. 저장 파일에는 재료를 이름(id)으로 적으니, 불러올 때 이름으로 재료를 찾는 데 쓴다.
/// 에셋은 메뉴 "유리병 정원 > 재료 목록 만들기"가 Assets/Resources/MaterialDatabase.asset 으로 만든다.
/// 재료를 새로 만들었으면 이 메뉴를 다시 누른다.
/// </summary>
public class MaterialDatabase : ScriptableObject
{
    public MaterialData[] materials;

    private static MaterialDatabase instance;
    private Dictionary<string, MaterialData> byId;

    public static MaterialDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<MaterialDatabase>("MaterialDatabase");
            return instance;
        }
    }

    /// <summary>이름(id)으로 재료를 찾는다. 없으면 null.</summary>
    public MaterialData Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (byId == null)
        {
            byId = new Dictionary<string, MaterialData>();
            foreach (MaterialData m in materials)
                if (m != null && !string.IsNullOrEmpty(m.id)) byId[m.id] = m;
        }
        byId.TryGetValue(id, out MaterialData found);
        return found;
    }

    // Play를 껐다 켤 때 기억해 둔 것을 버린다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }
}
