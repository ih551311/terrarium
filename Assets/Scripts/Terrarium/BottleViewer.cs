using UnityEngine;

/// <summary>
/// 병의 방향을 바꾼다. 병 뿌리 오브젝트(Bottle)에 붙인다.
/// 마우스 끌기는 패널 화면의 DragRotate가 받아서 Rotate()를 부른다.
/// - 좌우로 끌면 병이 옆으로 돌고, 위아래로 끌면 병이 앞뒤로 기운다. 속과 흙 윗면이 보이게 돌려 가며 꾸민다.
/// - 시점(카메라)은 정면에 고정되어 있고, 병만 돈다. 병 가운데는 제자리에 있는다.
/// - 처음에는 입구가 카메라 쪽으로 조금(tiltDegrees) 기울어 있어서 정면에서도 흙 윗면이 보인다. ResetOrientation()으로 처음 방향으로 돌아간다.
/// 병 카메라는 병의 자식이지만, 방향을 바꿀 때마다 제자리로 되돌려 놓는다.
/// </summary>
public class BottleViewer : MonoBehaviour
{
    [Tooltip("처음에 입구가 카메라 쪽으로 기울어지는 각도(도). 서 있는 병은 28 정도, 누운 병은 0")]
    [SerializeField] private float tiltDegrees;

    private Camera viewCamera;
    private BoxCollider bounds;
    private Quaternion baseRotation;   // 처음 놓인 방향
    private Vector3 cameraRight, cameraUp;  // 카메라 기준 축 (카메라는 고정이라 한 번만 구한다)
    private Vector3 centerLocal;       // 병 가운데 (병 기준)
    private Vector3 centerWorld;       // 병 가운데 (세계 기준, 돌려도 이 자리에 있는다)
    private Quaternion orientation = Quaternion.identity; // baseRotation 위에 얹는 세계 기준 회전
    private bool ready;

    private void Awake()
    {
        Prepare();
        ResetOrientation();
    }

    /// <summary>병 가운데를 기준으로 한 처음 상태를 기억한다.</summary>
    private void Prepare()
    {
        if (ready) return;
        ready = true;
        viewCamera = GetComponentInChildren<Camera>(true);
        bounds = GetComponent<BoxCollider>();
        baseRotation = transform.rotation;
        centerLocal = bounds != null ? bounds.center : Vector3.zero;
        centerWorld = transform.TransformPoint(centerLocal);
        cameraRight = viewCamera != null ? viewCamera.transform.right : Vector3.right;
        cameraUp = viewCamera != null ? viewCamera.transform.up : Vector3.up;
    }

    /// <summary>처음 방향(입구가 카메라 쪽으로 조금 기울어진 모습)으로 돌아간다.</summary>
    public void ResetOrientation()
    {
        Prepare();
        // 카메라 오른쪽 축으로 양수만큼 돌리면 병 위쪽이 카메라에서 멀어진다(흙 윗면이 등을 돌려서 옆면만 보이고, 흙 위의 재료 아랫부분이 앞쪽 흙에 가린다).
        // 입구가 카메라 쪽으로 오게 하려면 음수로 돌린다.
        orientation = Quaternion.AngleAxis(-tiltDegrees, cameraRight);
        Apply();
    }

    /// <summary>
    /// 병을 돌린다. yawDegrees가 양수면 병 앞면이 오른쪽으로, pitchDegrees가 양수면 병 앞면이 위로 간다.
    /// (마우스를 오른쪽/위로 끌면 양수)
    /// </summary>
    public void Rotate(float yawDegrees, float pitchDegrees = 0f)
    {
        Prepare();
        // 카메라 기준 축으로 돌려서, 어떤 방향이든 끈 쪽으로 병이 움직인다.
        orientation = Quaternion.AngleAxis(-yawDegrees, cameraUp) * Quaternion.AngleAxis(pitchDegrees, cameraRight) * orientation;
        Apply();
    }

    private void Apply()
    {
        Vector3 camPosition = Vector3.zero;
        Quaternion camRotation = Quaternion.identity;
        if (viewCamera != null) viewCamera.transform.GetPositionAndRotation(out camPosition, out camRotation);

        Quaternion rotation = orientation * baseRotation;
        transform.rotation = rotation;
        transform.position = centerWorld - rotation * centerLocal; // 병 가운데는 제자리

        if (viewCamera != null) viewCamera.transform.SetPositionAndRotation(camPosition, camRotation);
    }
}
