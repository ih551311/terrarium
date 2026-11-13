using UnityEngine;

/// <summary>
/// 섬에 놓인, 주울 수 있는 재료. 재료 오브젝트(또는 그 부모)에 붙이고 Collider가 있어야 한다.
/// 모델은 나중에 이 오브젝트의 자식을 바꿔 끼우면 된다.
/// </summary>
public class Pickup : MonoBehaviour
{
    [SerializeField] private MaterialData material;

    public MaterialData Material => material;

    /// <summary>주웠을 때 호출. 지금은 숨기기만 한다. (다음 날 다시 나기는 5단계)</summary>
    public void Collect()
    {
        gameObject.SetActive(false);
    }
}
