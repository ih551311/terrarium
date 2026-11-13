using UnityEngine;

/// <summary>
/// 병을 좌우로 돌린다. 병 뿌리 오브젝트(Bottle)에 붙인다.
/// 마우스 끌기는 패널 화면의 DragRotate가 받아서 Rotate()를 부른다.
/// 병 카메라는 병의 자식이지만, 병만 돌고 카메라는 제자리에 있어야 돌아가는 모습이 보인다.
///
/// 서 있는 병은 입구가 보는 쪽(카메라 쪽)으로 살짝 기울여 둔다. 그러면 정면에서 봐도 흙 윗면과 병 속이 보인다.
/// 돌릴 때는 기울인 채로 병 자신의 세로축을 기준으로 돈다. (누운 병은 기울이지 않는다)
/// </summary>
public class BottleViewer : MonoBehaviour
{
    [Tooltip("병 입구가 카메라 쪽으로 기울어지는 각도(도). 서 있는 병은 15 정도, 누운 병은 0")]
    [SerializeField] private float tiltDegrees;

    private Camera viewCamera;
    private BoxCollider bounds;
    private Quaternion baseRotation;   // 처음 놓인 방향
    private Vector3 tiltAxis;          // 기울일 축 (카메라의 오른쪽)
    private Vector3 centerLocal;       // 병 가운데 (병 기준)
    private Vector3 centerWorld;       // 병 가운데 (세계 기준, 돌려도 이 자리에 있는다)
    private float yaw;                 // 병 자신의 세로축으로 돌린 각도
    private bool ready;

    private void Awake()
    {
        Prepare();
        Apply();
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
        tiltAxis = viewCamera != null ? viewCamera.transform.right : Vector3.right;
    }

    /// <summary>병을 앞으로 기울이고, 그 상태에서 yaw만큼 돌린 모습으로 놓는다. 가운데는 제자리, 카메라도 제자리.</summary>
    private void Apply()
    {
        Vector3 camPosition = Vector3.zero;
        Quaternion camRotation = Quaternion.identity;
        if (viewCamera != null) viewCamera.transform.GetPositionAndRotation(out camPosition, out camRotation);

        // 입구가 카메라 쪽으로 오도록 카메라 오른쪽 축을 기준으로 눕힌다 (양수 각도 = 위쪽이 나에게 온다)
        Quaternion tilt = Quaternion.AngleAxis(tiltDegrees, tiltAxis);
        Quaternion spin = Quaternion.AngleAxis(yaw, baseRotation * Vector3.up);
        Quaternion rotation = tilt * spin * baseRotation;

        transform.rotation = rotation;
        transform.position = centerWorld - rotation * centerLocal;

        if (viewCamera != null) viewCamera.transform.SetPositionAndRotation(camPosition, camRotation);
    }

    /// <summary>병을 자신의 세로축 기준으로 돌린다. 양수는 오른쪽으로.</summary>
    public void Rotate(float degrees)
    {
        Prepare();
        yaw -= degrees;
        Apply();
    }
}
