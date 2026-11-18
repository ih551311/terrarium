using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 넘어가도 남는 것들(플레이어, 가방, UI, EventSystem)을 담은 뿌리 오브젝트.
/// 모든 씬에 이 프리팹을 하나씩 넣어 둔다. 이미 있으면 새로 들어온 쪽은 스스로 사라진다.
/// 그래서 어느 씬에서 Play를 눌러도 시작할 수 있다.
/// </summary>
[DefaultExecutionOrder(-2000)] // EventSystem(-1000)보다 먼저 깨어나서, 중복이면 자식들이 켜지기 전에 스스로 꺼진다
public class GameRoot : MonoBehaviour
{
    [Tooltip("플레이어 오브젝트 (씬을 옮길 때 도착 지점으로 이동시킨다)")]
    [SerializeField] private Transform player;
    [SerializeField] private BottlePanel bottlePanel;
    [SerializeField] private MailboxPanel mailboxPanel;
    [SerializeField] private BottleDetailPanel bottleDetailPanel;

    public static GameRoot Instance { get; private set; }
    public BottlePanel BottlePanel => bottlePanel;
    public BottleDetailPanel BottleDetailPanel => bottleDetailPanel;
    public MailboxPanel MailboxPanel => mailboxPanel;
    public Transform Player => player;

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
        lastAutoSave = Time.realtimeSinceStartup;

        // 저장이 있으면 가방을 저장된 내용으로 채운다 (날짜, 코인, 병은 장면이 열리기 전에 이미 채워졌다)
        if (player != null) SaveSystem.ApplyBag(player.GetComponent<Bag>());
    }

    // 게임을 처음 켰을 때는 집 앞(FromHut 도착 지점)에서 시작한다. (다른 씬에서 넘어온 것이 아닐 때만)
    private void Start()
    {
        if (string.IsNullOrEmpty(pendingSpawnId) && player != null) GoHome();
    }

    private const float AutoSaveSeconds = 120f; // 이 시간마다 저절로 저장한다 (실제 시간)
    private float lastAutoSave;
    private bool quitting;

    private void Update()
    {
        // 하루의 시간이 흐른다 (집 안에서도 흐르고, 밤 끝에서 멈춘다)
        GameState.Tick(Time.deltaTime);

        // 가끔 저절로 저장한다 (패널이 열려 있지 않을 때)
        if (Time.realtimeSinceStartup - lastAutoSave > AutoSaveSeconds && !UIFocus.IsOpen)
            SaveNow();
    }

    /// <summary>지금 상태를 저장한다. 성공하면 true. (읽지 못한 저장 파일이 있으면 덮어쓰지 않는다)</summary>
    public bool SaveNow()
    {
        lastAutoSave = Time.realtimeSinceStartup;
        if (SaveSystem.LoadFailed) return false;
        Bag bag = player != null ? player.GetComponent<Bag>() : null;
        return SaveSystem.Save(bag);
    }

    // 게임을 끄거나 앱이 멈출 때 저절로 저장한다 (침대에서 자지 않고 꺼도 진행이 남는다)
    private void OnApplicationQuit()
    {
        if (Instance != this || quitting) return;
        quitting = true;
        SaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && Instance == this) SaveNow();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    /// <summary>다른 씬으로 가서 spawnId 이름의 도착 지점에 선다. 가는 동안 재료 쪽지가 있는 로딩 화면이 뜬다(LoadingScreen).</summary>
    public void GoTo(string sceneName, string spawnId)
    {
        if (travelling) return; // 이미 가는 중
        pendingSpawnId = spawnId;
        SaveNow(); // 집을 드나들 때도 저장한다
        StartCoroutine(Travel(sceneName));
    }

    private bool travelling;

    /// <summary>저장하고 메인 화면으로 돌아간다. 이 뿌리 오브젝트(플레이어, UI)는 지우고, 다시 시작하면 새로 만들어진다.</summary>
    public void ReturnToMenu()
    {
        if (travelling) return;
        SaveNow();
        Time.timeScale = 1f;
        Instance = null;
        gameObject.SetActive(false); // 지워지기 전 한 프레임 동안 소리 듣는 귀가 둘이 되지 않게 먼저 끈다
        Destroy(gameObject);
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>지금 다른 씬으로 가는 중인가 (로딩 화면이 떠 있다)</summary>
    public bool IsTravelling => travelling;

    private System.Collections.IEnumerator Travel(string sceneName)
    {
        travelling = true;
        UIFocus.Open(); // 가는 동안 걷기와 줍기를 멈춘다
        yield return LoadingScreen.Show(sceneName);
        float shownAt = Time.realtimeSinceStartup;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        while (Time.realtimeSinceStartup - shownAt < LoadingScreen.MinSeconds) yield return null;
        UIFocus.Close();
        yield return LoadingScreen.Hide();
        travelling = false;
    }

    /// <summary>
    /// 지금 있는 씬의 "집 앞"(id가 FromHut인 도착 지점)으로 바로 간다. 섬에서 쓴다.
    /// 그런 자리가 없는 씬(집 안)에서는 아무 일도 하지 않고 false를 돌려준다.
    /// </summary>
    public bool GoHome(string spawnId = "FromHut")
    {
        foreach (SpawnPoint spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (spawn.Id != spawnId) continue;
            Teleport(spawn.transform);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 섬의 (x, z) 자리로 이동한다. 지형 높이에 맞춰 서고, 그 자리가 바다이거나 나무와 바위로 막혀 있으면
    /// 가까운 빈 자리를 찾아 보고, 그래도 없으면 이동하지 않고 false를 돌려준다.
    /// </summary>
    public bool TeleportTo(Vector2 xz)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return false;

        // 가까운 자리부터 차례로 시험한다 (나무 한가운데에 서지 않도록)
        Vector2[] offsets =
        {
            Vector2.zero, new Vector2(2f, 0f), new Vector2(-2f, 0f), new Vector2(0f, 2f), new Vector2(0f, -2f),
            new Vector2(3f, 3f), new Vector2(-3f, 3f), new Vector2(3f, -3f), new Vector2(-3f, -3f),
            new Vector2(5f, 0f), new Vector2(-5f, 0f), new Vector2(0f, 5f), new Vector2(0f, -5f)
        };

        foreach (Vector2 offset in offsets)
        {
            Vector2 p = xz + offset;
            float ground = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
            if (ground < 0.6f) continue; // 바다

            Vector3 bottom = new Vector3(p.x, ground + 0.5f, p.y);
            if (Physics.CheckCapsule(bottom, bottom + Vector3.up * 1.2f, 0.35f)) continue; // 막힌 곳

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.position = new Vector3(p.x, ground + 1f, p.y);
            if (controller != null) controller.enabled = true;
            return true;
        }
        return false;
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
