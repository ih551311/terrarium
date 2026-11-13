using UnityEngine;

/// <summary>
/// 집 안에 놓인 병. 바라보고 E를 누르면 병 꾸미기 패널이 열린다.
/// 병 뿌리 오브젝트(Bottle)에 붙이고, 바라볼 수 있도록 Collider(Is Trigger)가 있어야 한다.
/// </summary>
[RequireComponent(typeof(BottleViewer))]
public class BottleObject : MonoBehaviour, IInteractable
{
    [Tooltip("이 병만 비추는 카메라 (RenderTexture로 출력). 평소엔 꺼 둔다.")]
    [SerializeField] private Camera bottleCamera;

    public string Prompt => "[E] 병 꾸미기";

    public void Interact(Interactor who)
    {
        GameRoot.Instance.BottlePanel.Open(GetComponent<BottleViewer>(), bottleCamera);
    }
}
