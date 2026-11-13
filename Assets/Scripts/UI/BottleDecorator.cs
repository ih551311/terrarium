using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 병 꾸미기 패널 안 병 그림(RawImage) 위에서 재료를 놓고, 돌리고, 키우고, 빼는 조작을 받는다.
///   좌클릭: 고른 재료를 클릭한 자리(바닥재 표면)에 놓기
///   마우스를 올린 재료에 대해 → 휠: 돌리기, Q / R: 작게 / 크게, 우클릭: 빼기(가방으로 돌아감)
/// (끌어서 병 돌리기는 DragRotate가 맡는다.)
/// 병 그림 오브젝트에 붙인다. 어떤 병인지는 패널이 열릴 때 SetTarget으로 받는다.
/// </summary>
public class BottleDecorator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IScrollHandler
{
    [SerializeField] private Bag bag;
    [SerializeField] private MaterialStrip strip;
    [Tooltip("\"병이 가득 찼어요\" 같은 안내를 잠깐 보여 주는 글자")]
    [SerializeField] private TMP_Text messageText;

    [Tooltip("이만큼(픽셀) 넘게 끌었으면 클릭이 아니라 끌기로 본다")]
    [SerializeField] private float clickMaxPixels = 10f;
    [SerializeField] private float rotateDegreesPerScroll = 15f;
    [SerializeField] private float scaleStep = 1.1f;
    [SerializeField] private float messageSeconds = 2f;

    private RectTransform rect;
    private BottleContents contents;
    private Camera bottleCamera;
    private Vector2 downPosition;
    private float messageUntil;

    private void Awake()
    {
        rect = (RectTransform)transform;
        if (messageText != null) messageText.text = string.Empty;
    }

    /// <summary>다룰 병과, 그 병을 비추는 카메라를 받는다. 닫을 때는 (null, null).</summary>
    public void SetTarget(BottleContents bottleContents, Camera camera)
    {
        contents = bottleContents;
        bottleCamera = camera;
        if (messageText != null) messageText.text = string.Empty;
    }

    private void Update()
    {
        if (messageText != null && messageUntil > 0f && Time.time > messageUntil)
        {
            messageText.text = string.Empty;
            messageUntil = 0f;
        }

        if (contents == null) return;

        // Q / R: 마우스를 올린 재료를 작게 / 크게
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        bool smaller = keyboard.qKey.wasPressedThisFrame;
        bool bigger = keyboard.rKey.wasPressedThisFrame;
        if (!smaller && !bigger) return;

        PlacedMaterial hovered = FindHovered();
        if (hovered == null) return;
        hovered.ScaleBy(bigger ? scaleStep : 1f / scaleStep);
        contents.SaveState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        downPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (contents == null || bottleCamera == null) return;
        // 많이 움직였다면 끌기(병 돌리기)였으니 놓지 않는다.
        if (Vector2.Distance(eventData.position, downPosition) > clickMaxPixels) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, null)) return;

        if (eventData.button == PointerEventData.InputButton.Left) TryPlace(eventData.position);
        else if (eventData.button == PointerEventData.InputButton.Right) TryRemove(eventData.position);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (contents == null) return;
        PlacedMaterial hovered = FindHovered();
        if (hovered == null) return;

        hovered.Rotate(Mathf.Sign(eventData.scrollDelta.y) * rotateDegreesPerScroll);
        contents.SaveState();
    }

    private void TryPlace(Vector2 screenPosition)
    {
        MaterialData selected = strip != null ? strip.Selected : null;
        if (selected == null) { ShowMessage("아래에서 재료를 먼저 골라 주세요"); return; }
        if (contents.IsFull) { ShowMessage("병이 가득 찼어요"); return; }

        if (!TryGetRay(screenPosition, out Ray ray) ||
            !contents.TryGetSurfacePoint(ray, out Vector3 localPoint))
        {
            ShowMessage("병 안 흙 위를 눌러 주세요");
            return;
        }

        if (!bag.TryRemove(selected)) return; // 가방에 없으면 놓지 않는다
        contents.Place(selected, localPoint);
    }

    private void TryRemove(Vector2 screenPosition)
    {
        PlacedMaterial target = FindHovered(screenPosition);
        if (target == null) return;

        // 가방에 자리가 없으면 빼지 않는다 (재료가 사라지지 않게).
        if (!bag.TryAdd(target.Material)) { ShowMessage("가방이 가득 찼어요"); return; }
        contents.Remove(target);
    }

    private PlacedMaterial FindHovered()
    {
        Mouse mouse = Mouse.current;
        return mouse == null ? null : FindHovered(mouse.position.ReadValue());
    }

    /// <summary>화면의 한 점 아래에 있는, 병 안에 놓인 재료를 찾는다. 없으면 null.</summary>
    private PlacedMaterial FindHovered(Vector2 screenPosition)
    {
        if (bottleCamera == null || !TryGetRay(screenPosition, out Ray ray)) return null;

        // 병 자체의 Collider(문 앞에서 바라볼 때 쓰는 것)가 앞에 있어도 뚫고, 가장 가까운 재료를 고른다.
        RaycastHit[] hits = Physics.RaycastAll(ray, 10f, 1 << contents.gameObject.layer, QueryTriggerInteraction.Collide);
        PlacedMaterial best = null;
        float bestDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            PlacedMaterial placed = hit.collider.GetComponentInParent<PlacedMaterial>();
            if (placed != null && hit.distance < bestDistance)
            {
                best = placed;
                bestDistance = hit.distance;
            }
        }
        return best;
    }

    /// <summary>병 그림 위의 화면 좌표를, 병 카메라에서 쏘는 광선으로 바꾼다.</summary>
    private bool TryGetRay(Vector2 screenPosition, out Ray ray)
    {
        ray = default;
        if (bottleCamera == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out Vector2 local))
            return false;

        Rect r = rect.rect;
        Vector2 viewport = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;

        ray = bottleCamera.ViewportPointToRay(viewport);
        return true;
    }

    private void ShowMessage(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageUntil = Time.time + messageSeconds;
    }
}
