using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 집 안 선반. 사용자가 전시하기로 고른 완성한 병이 비어 있는 가장 앞 칸에 올라가고, 병 속이 그대로 보인다.
/// 칸 자리(slots)는 InteriorSetup이 선반 위에 놓아 둔다. 병은 작업대의 병과 같은 병 프리팹을 작게 줄여 복사해서 올린다.
/// 선반은 완성한 병을 전시하는 곳이다. (식물은 자라지 않는다)
/// </summary>
public class BottleShelf : MonoBehaviour
{
    [Tooltip("칸마다 하나씩, 앞 칸부터. 위치는 병 바닥이 닿는 자리다.")]
    [SerializeField] private Transform[] slots;

    [Tooltip("병 프리팹을 선반 크기로 줄이는 비율 (병 크기 2배인 병을 0.55배로)")]
    [SerializeField] private float bottleScale = 0.55f;

    [Tooltip("이 병 종류들. 병 번호(bottleId)로 프리팹을 찾는다. 작업대의 BottleStation에서 받는다.")]
    [SerializeField] private BottleStation station;

    private readonly Dictionary<string, GameObject> shown = new Dictionary<string, GameObject>();

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        BottleStore.Changed += Refresh;
    }

    private void OnDisable()
    {
        BottleStore.Changed -= Refresh;
    }

    /// <summary>
    /// 전시하기로 한 병(완성했고 칸 번호가 있는 병)만 선반에 올리고, 내린 병은 치운다.
    /// 병을 전시하고 내리는 것은 병 꾸미기 화면의 버튼이나 선반 병에서 E를 눌러 한다.
    /// </summary>
    private void Refresh()
    {
        if (station == null) return;
        for (int i = 0; i < station.Count; i++)
        {
            BottleContents bottle = station.Get(i);
            string id = bottle.BottleId;
            BottleRecord record = BottleStore.Get(id);
            bool shouldShow = record.completed && record.shelfSlot >= 0 && record.shelfSlot < slots.Length;

            if (!shouldShow)
            {
                if (shown.TryGetValue(id, out GameObject old))
                {
                    if (old != null) { old.SetActive(false); Destroy(old); }
                    shown.Remove(id);
                }
                continue;
            }
            if (shown.ContainsKey(id)) continue;
            shown[id] = Place(bottle, record.shelfSlot);
        }
    }

    private GameObject Place(BottleContents source, int slotIndex)
    {
        // 작업대의 병과 같은 병 프리팹을 복사한다. (병 번호가 같아서 병 속은 기록에서 저절로 되살아난다)
        GameObject prefab = source.Prefab;
        Transform slot = slots[slotIndex];
        GameObject copy = Instantiate(prefab, slot.position, slot.rotation, transform);
        copy.name = "ShelfBottle_" + source.BottleId;

        // 선반에서는 돌리거나 카메라로 보지 않는다
        BottleViewer viewer = copy.GetComponent<BottleViewer>();
        if (viewer != null) Destroy(viewer);
        Camera cam = copy.GetComponentInChildren<Camera>(true);
        if (cam != null) Destroy(cam.gameObject);

        // 크기를 줄이고, 병 바닥(원점)이 칸 자리에 닿게 한다. 누운 병은 옆으로 길어서 선반 안쪽을 향하게 돌린다.
        copy.transform.SetPositionAndRotation(slot.position, slot.rotation);
        copy.transform.localScale = Vector3.one * bottleScale;
        if (copy.GetComponent<BottleContents>().IsSideways)
            copy.transform.rotation = slot.rotation * Quaternion.Euler(0f, 90f, 0f);

        copy.AddComponent<ShelfBottle>().Init(source.BottleId, source.DisplayName);
        return copy;
    }
}
