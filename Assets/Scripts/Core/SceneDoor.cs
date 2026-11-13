using UnityEngine;

/// <summary>
/// 다른 씬으로 가는 문. 바라보고 E를 누르면 targetScene으로 가서 spawnId 자리에 선다.
/// Collider가 있는 오브젝트에 붙인다. 가려는 씬은 Build Settings에 들어 있어야 한다.
/// </summary>
public class SceneDoor : MonoBehaviour, IInteractable
{
    [Tooltip("가려는 씬 이름. 예: HutInterior")]
    [SerializeField] private string targetScene;

    [Tooltip("그 씬에서 설 자리(SpawnPoint)의 id")]
    [SerializeField] private string spawnId;

    [Tooltip("안내 글자. 예: 집에 들어가기")]
    [SerializeField] private string label = "문 열기";

    public string Prompt => "[E] " + label;

    public void Interact(Interactor who)
    {
        GameRoot.Instance.GoTo(targetScene, spawnId);
    }
}
