using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 넘어가도 남는 것들(플레이어, 가방, UI, EventSystem)을 담은 뿌리 오브젝트.
/// 모든 씬에 이 프리팹을 하나씩 넣어 둔다. 이미 있으면 새로 들어온 쪽은 스스로 사라진다.
/// 그래서 어느 씬에서 Play를 눌러도 시작할 수 있다.
/// </summary>
public class GameRoot : MonoBehaviour
{
    [Tooltip("플레이어 오브젝트 (씬을 옮길 때 도착 지점으로 이동시킨다)")]
    [SerializeField] private Transform player;
    [SerializeField] private BottlePanel bottlePanel;

    public static GameRoot Instance { get; private set; }
    public BottlePanel BottlePanel => bottlePanel;

    private string pendingSpawnId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 이미 하나 있다: 중복은 지운다. 지워지기 전 한 프레임 동안 EventSystem이나 오디오 리스너가
            // 두 개로 잡히지 않게, 먼저 꺼 둔다.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    /// <summary>다른 씬으로 가서 spawnId 이름의 도착 지점에 선다.</summary>
    public void GoTo(string sceneName, string spawnId)
    {
        pendingSpawnId = spawnId;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(pendingSpawnId)) return;

        foreach (SpawnPoint spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (spawn.Id != pendingSpawnId) continue;
            Teleport(spawn.transform);
            break;
        }
        pendingSpawnId = null;
    }

    private void Teleport(Transform target)
    {
        // CharacterController는 켜진 채로는 위치를 직접 바꿀 수 없어서 잠깐 끈다.
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.SetPositionAndRotation(target.position, Quaternion.Euler(0f, target.eulerAngles.y, 0f));
        if (controller != null) controller.enabled = true;
    }

    // Play를 껐다 켤 때 값이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }
}
