using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 패널 안 병 그림(RawImage) 위에서 마우스 왼쪽 버튼으로 끌면 병의 방향이 바뀐다.
/// 좌우로 끌면 병이 돌고, 위아래로 끌면 병이 앞뒤로 기운다. (시점은 정면 고정, 병만 움직인다)
/// T 키를 누르면 병이 처음 방향으로 돌아간다.
/// 어떤 병을 돌릴지는 패널이 열릴 때 SetTarget으로 받는다. 병 그림 오브젝트에 붙인다.
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

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && bottle != null && keyboard.tKey.wasPressedThisFrame) bottle.ResetOrientation();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (bottle != null) bottle.Rotate(eventData.delta.x * degreesPerPixel, eventData.delta.y * degreesPerPixel);
    }
}
