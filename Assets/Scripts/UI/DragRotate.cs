using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 패널 안 병 그림(RawImage) 위에서 마우스 왼쪽 버튼으로 끌면 병이 돈다.
/// 어떤 병을 돌릴지는 패널이 열릴 때 SetTarget으로 받는다.
/// 병 그림 오브젝트에 붙인다.
/// </summary>
public class DragRotate : MonoBehaviour, IDragHandler
{
    [Tooltip("마우스를 한 픽셀 끌 때 도는 각도")]
    [SerializeField] private float degreesPerPixel = 0.4f;

    private BottleViewer bottle;

    public void SetTarget(BottleViewer target)
    {
        bottle = target;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (bottle != null) bottle.Rotate(eventData.delta.x * degreesPerPixel);
    }
}
