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

    [Header("확대 / 축소 (재료가 없는 곳에서 휠)")]
    [Tooltip("휠 한 칸에 바뀌는 시야각(도). 시야각이 작을수록 크게 보인다.")]
    [SerializeField] private float zoomStepDegrees = 6f;
    [SerializeField] private float minFieldOfView = 20f;
    [SerializeField] private float maxFieldOfView = 75f;
    [Tooltip("클수록 빨리 따라간다")]
    [SerializeField] private float zoomSmoothing = 14f;

    private RectTransform rect;
    private BottleContents contents;
    private Camera bottleCamera;
    private Vector2 downPosition;
    private float messageUntil;
    private float defaultFieldOfView;
    private float targetFieldOfView;
    private Quaternion defaultRotation;

    private void Awake()
    {
        rect = (RectTransform)transform;
        if (messageText != null) messageText.text = string.Empty;
    }

    /// <summary>다룰 병과, 그 병을 비추는 카메라를 받는다. 닫을 때는 (null, null).</summary>
    public void SetTarget(BottleContents bottleContents, Camera camera)
    {
        if (contents != null) contents.HideGhost(); // 닫을 때 미리보기가 남지 않게
        if (bottleCamera != null)
        {
            // 확대한 것을 원래대로
            bottleCamera.fieldOfView = defaultFieldOfView;
            bottleCamera.transform.rotation = defaultRotation;
        }

        contents = bottleContents;
        bottleCamera = camera;
        if (bottleCamera != null)
        {
            defaultFieldOfView = bottleCamera.fieldOfView;
            defaultRotation = bottleCamera.transform.rotation;
            targetFieldOfView = defaultFieldOfView; // 열 때마다 기본 크기로 시작
        }
        if (messageText != null) messageText.text = string.Empty;
    }

    private void Update()
    {
        if (messageText != null && messageUntil > 0f && Time.time > messageUntil)
        {
            messageText.text = string.Empty;
            messageUntil = 0f;
        }

        // 확대 / 축소: 목표 시야각으로 부드럽게 따라간다.
        if (bottleCamera != null)
        {
            float t = 1f - Mathf.Exp(-zoomSmoothing * Time.unscaledDeltaTime);
            bottleCamera.fieldOfView = Mathf.Lerp(bottleCamera.fieldOfView, targetFieldOfView, t);

            // 확대할수록 시선을 흙 쪽으로 내려서, 재료를 놓는 바닥이 잘리지 않게 한다.
            if (contents != null)
            {
                float zoomIn = Mathf.InverseLerp(defaultFieldOfView, minFieldOfView, bottleCamera.fieldOfView);
                Vector3 lookAt = contents.transform.TransformPoint(contents.ZoomLookPoint); // 병마다 다르다
                Quaternion toGround = Quaternion.LookRotation(lookAt - bottleCamera.transform.position);
                bottleCamera.transform.rotation = Quaternion.Slerp(defaultRotation, toGround, zoomIn);
            }
        }

        if (contents == null) return;

        Mouse mouse = Mouse.current;
        UpdateGhost(mouse != null ? mouse.position.ReadValue() : (Vector2?)null);

        // Q / R: 마우스를 올린 재료를 작게 / 크게
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || contents.IsCompleted) return;
        bool smaller = keyboard.qKey.wasPressedThisFrame;
        bool bigger = keyboard.rKey.wasPressedThisFrame;
        if (!smaller && !bigger) return;

        PlacedMaterial hovered = FindHovered();
        if (hovered == null) return;
        // 병 밖으로 나가면 안쪽으로 밀어 보고, 그래도 안 되면 키우지 않는다.
        if (!contents.TryScale(hovered, bigger ? scaleStep : 1f / scaleStep) && bigger)
            ShowMessage("병 안에서 더 키울 수 없어요");
    }

    /// <summary>
    /// 고른 재료를 지금 클릭하면 놓일 자리에 반투명한 그림자를 보여 준다.
    /// 재료를 안 골랐거나, 병이 가득 찼거나, 마우스가 병 안 바닥에서 멀거나, 이 병에 안 들어가면 숨긴다.
    /// 재료가 병 밖으로 나가는 자리에서는 그림자가 안쪽으로 밀려 들어간 자리에 보인다(놓이는 자리와 같다).
    /// </summary>
    private void UpdateGhost(Vector2? screenPosition)
    {
        MaterialData selected = strip != null ? strip.Selected : null;
        if (selected != null && selected.kind != MaterialKind.Ground &&
            !contents.IsFull && !contents.IsCompleted && screenPosition.HasValue &&
            TryGetRay(screenPosition.Value, out Ray ray) &&
            contents.TryFindPlacement(selected, ray, out Vector3 localPoint) == PlacementResult.Ok)
        {
            contents.ShowGhost(selected, localPoint);
        }
        else
        {
            contents.HideGhost();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        downPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (contents == null || bottleCamera == null || contents.IsCompleted) return;
        // 많이 움직였다면 끌기(병 돌리기)였으니 놓지 않는다.
        if (Vector2.Distance(eventData.position, downPosition) > clickMaxPixels) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, null)) return;

        if (eventData.button == PointerEventData.InputButton.Left) TryPlace(eventData.position);
        else if (eventData.button == PointerEventData.InputButton.Right) TryRemove(eventData.position);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (contents == null || Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;
        float direction = Mathf.Sign(eventData.scrollDelta.y);

        // 놓은 재료 위에서는 그 재료를 돌린다. (완성한 병은 고칠 수 없다)
        PlacedMaterial hovered = contents.IsCompleted ? null : FindHovered(eventData.position);
        if (hovered != null)
        {
            // 돌린 모습이 병 밖으로 나가면 안쪽으로 밀어 보고, 그래도 안 되면 돌리지 않는다.
            if (!contents.TryRotate(hovered, direction * rotateDegreesPerScroll))
                ShowMessage("병 안에서 더 돌릴 수 없어요");
            return;
        }

        // 그 밖의 곳에서는 병 화면을 확대(휠 위로) / 축소(휠 아래로)한다.
        targetFieldOfView = Mathf.Clamp(targetFieldOfView - direction * zoomStepDegrees, minFieldOfView, maxFieldOfView);
    }

    private void TryPlace(Vector2 screenPosition)
    {
        MaterialData selected = strip != null ? strip.Selected : null;
        if (selected == null) { ShowMessage("아래에서 재료를 먼저 골라 주세요"); return; }
        if (selected.kind == MaterialKind.Ground) { ShowMessage("바닥재는 오른쪽 바닥재에서 골라 주세요"); return; }
        if (contents.IsFull) { ShowMessage("병이 가득 찼어요"); return; }

        Vector3 localPoint = Vector3.zero;
        PlacementResult result = TryGetRay(screenPosition, out Ray ray)
            ? contents.TryFindPlacement(selected, ray, out localPoint)
            : PlacementResult.Outside;

        if (result == PlacementResult.Outside) { ShowMessage("병 안 흙 위를 눌러 주세요"); return; }
        if (result == PlacementResult.TooBig) { ShowMessage("이 재료는 이 병에 들어가지 않아요"); return; }

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

    /// <summary>화면 위쪽에 안내 글자를 잠깐 보여 준다.</summary>
    public void ShowMessage(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageUntil = Time.time + messageSeconds;
    }
}
