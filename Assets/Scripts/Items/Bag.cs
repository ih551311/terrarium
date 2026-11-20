using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가방. 칸 수는 UISetup이 정한다(96칸, 화면에서 휠로 내려 본다). 같은 재료는 한 칸에 5개까지 겹친다.
/// Player 오브젝트에 붙인다.
/// </summary>
public class Bag : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public MaterialData material;
        public int count;
    }

    [SerializeField] private int slotCount = 96;
    [SerializeField] private int maxStack = 5;

    private readonly List<Slot> slots = new List<Slot>();

    public IReadOnlyList<Slot> Slots => slots;
    public int SlotCount => slotCount;

    /// <summary>가방 내용이 바뀔 때마다 호출된다. (화면 갱신용)</summary>
    public event System.Action Changed;

    /// <summary>가방을 주어진 내용으로 바꾼다. (저장을 불러올 때) 칸 수와 한 칸의 최대 개수를 넘는 것은 버린다.</summary>
    public void Restore(List<(MaterialData material, int count)> entries)
    {
        slots.Clear();
        foreach ((MaterialData material, int count) in entries)
        {
            if (slots.Count >= slotCount) break;
            slots.Add(new Slot { material = material, count = Mathf.Clamp(count, 1, maxStack) });
        }
        Changed?.Invoke();
    }

    /// <summary>재료 1개를 넣는다. 자리가 없으면 false.</summary>
    public bool TryAdd(MaterialData material)
    {
        if (material == null) return false;

        // 1) 같은 재료가 있고 아직 덜 찬 칸에 먼저 겹친다.
        foreach (Slot slot in slots)
        {
            if (slot.material == material && slot.count < maxStack)
            {
                slot.count++;
                Changed?.Invoke();
                GameAudio.Play(GameAudio.Sfx.Pickup, 0.7f);
                GameState.Discover(material);
                return true;
            }
        }

        // 2) 없으면 새 칸을 쓴다.
        if (slots.Count < slotCount)
        {
            slots.Add(new Slot { material = material, count = 1 });
            Changed?.Invoke();
            GameAudio.Play(GameAudio.Sfx.Pickup, 0.7f);
            GameState.Discover(material);
            return true;
        }

        return false; // 가방이 가득 참
    }

    /// <summary>재료 1개를 꺼낸다. 없으면 false. 맨 뒤 칸부터 줄어든다.</summary>
    public bool TryRemove(MaterialData material)
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].material != material) continue;

            slots[i].count--;
            if (slots[i].count <= 0) slots.RemoveAt(i);
            Changed?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>가방에 든 이 재료의 총 개수</summary>
    public int CountOf(MaterialData material)
    {
        int total = 0;
        foreach (Slot slot in slots)
            if (slot.material == material) total += slot.count;
        return total;
    }

    /// <summary>이 재료를 넣을 자리가 있는지 (넣지는 않는다).</summary>
    public bool CanAdd(MaterialData material)
    {
        if (material == null) return false;
        foreach (Slot slot in slots)
            if (slot.material == material && slot.count < maxStack) return true;
        return slots.Count < slotCount;
    }
}
