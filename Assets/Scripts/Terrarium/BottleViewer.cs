using UnityEngine;

/// <summary>
/// 병을 좌우로 돌린다. 병 뿌리 오브젝트(Bottle)에 붙인다.
/// 마우스 끌기는 패널 화면의 DragRotate가 받아서 Rotate()를 부른다.
/// </summary>
public class BottleViewer : MonoBehaviour
{
    /// <summary>병을 세로축 기준으로 돌린다. 양수는 오른쪽으로.</summary>
    public void Rotate(float degrees)
    {
        transform.Rotate(0f, -degrees, 0f, Space.World);
    }
}
