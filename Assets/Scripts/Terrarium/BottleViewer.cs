using UnityEngine;

/// <summary>
/// 병을 좌우로 돌린다. 병 뿌리 오브젝트(Bottle)에 붙인다.
/// 마우스 끌기는 패널 화면의 DragRotate가 받아서 Rotate()를 부른다.
/// 병 카메라는 병의 자식이지만, 병만 돌고 카메라는 제자리에 있어야 돌아가는 모습이 보인다.
/// </summary>
public class BottleViewer : MonoBehaviour
{
    private Camera viewCamera;
    private BoxCollider bounds;

    /// <summary>병을 가운데를 지나는 세로축 기준으로 돌린다. 양수는 오른쪽으로.</summary>
    public void Rotate(float degrees)
    {
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>(true);
        if (bounds == null) bounds = GetComponent<BoxCollider>();

        Vector3 pivot = bounds != null ? transform.TransformPoint(bounds.center) : transform.position;

        // 카메라는 돌리기 전 자리와 방향 그대로 되돌려 놓는다 (병과 함께 돌지 않게)
        Vector3 camPosition = Vector3.zero;
        Quaternion camRotation = Quaternion.identity;
        if (viewCamera != null) viewCamera.transform.GetPositionAndRotation(out camPosition, out camRotation);

        transform.RotateAround(pivot, Vector3.up, -degrees);

        if (viewCamera != null) viewCamera.transform.SetPositionAndRotation(camPosition, camRotation);
    }
}
