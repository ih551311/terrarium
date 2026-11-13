using UnityEngine;

/// <summary>
/// 임시: 가방 내용을 화면 왼쪽 위에 글자로 보여 준다. (확인용)
/// 정식 가방 화면(Tab)은 11단계에서 만들고 이 파일은 지운다.
/// Player 오브젝트에 붙인다.
/// </summary>
[RequireComponent(typeof(Bag))]
public class BagDebugHud : MonoBehaviour
{
    private Bag bag;

    private void Awake()
    {
        bag = GetComponent<Bag>();
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { font = GameFont.Main, fontSize = 36 };
        string text = "가방 (" + bag.Slots.Count + "/" + bag.SlotCount + "칸)\n";
        foreach (Bag.Slot slot in bag.Slots)
            text += slot.material.displayName + " x" + slot.count + "\n";
        GUI.Label(new Rect(15f, 15f, 400f, 400f), text, style);
    }
}
