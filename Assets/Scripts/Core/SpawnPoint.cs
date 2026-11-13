using UnityEngine;

/// <summary>
/// 씬에서 플레이어가 도착해 설 자리. id가 문(SceneDoor)의 spawnId와 같으면 거기에 선다.
/// 이 오브젝트의 파란 화살표(앞쪽)가 도착했을 때 바라보는 방향이다.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward);
    }
}
