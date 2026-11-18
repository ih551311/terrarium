using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 자세히 보기. 선반의 병을 바라보고 E를 누르거나, Tab 가방의 병 탭에서 병을 누르면 열린다.
/// 완성한 병을 크게 보여 주고, 마우스로 끌면 돌려 볼 수 있다. (T: 처음 방향) 이름과 종류, 선반에 놓였는지도 보인다.
/// 병은 작업대의 병과 같은 병 프리팹을 복사해 눈에 안 보이는 먼 곳에 놓고, 병 카메라가 RenderTexture에 그려서
/// 패널 안 RawImage로 보여 준다. (병 꾸미기 패널이 쓰는 것과 같은 그림 판이고, 두 패널이 동시에 열리지는 않는다)
/// 프리팹을 이 패널이 직접 들고 있어서 집 안과 밖 어디서든 열린다.
/// 패널 화면(UISetup이 만든다)은 GameRoot 안에 있고, 이 스크립트는 Canvas 오브젝트(UI)에 붙는다.
/// </summary>
public class BottleDetailPanel : MonoBehaviour
{
    [Tooltip("켜고 끄는 패널 화면 전체")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private DragRotate dragRotate;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text infoLabel;
    [Tooltip("선반에 올리거나 내리는 버튼")]
    [SerializeField] private Button shelfButton;
    [SerializeField] private TMP_Text shelfLabel;
    [Tooltip("병을 지우는 버튼 (두 번 눌러야 지워진다)")]
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text deleteLabel;

    [Tooltip("병 종류별 병 프리팹들 (UISetup이 Assets/Prefabs/Bottles에서 모아 넣는다). 작업대가 없는 섬에서도 병을 보려고 직접 들고 있는다.")]
    [SerializeField] private GameObject[] bottlePrefabs;

    [Tooltip("병을 복사해 놓는 자리 (병 카메라는 병 레이어만 그리므로 눈에 띄지 않는 먼 곳이면 된다)")]
    [SerializeField] private Vector3 viewSpot = new Vector3(0f, -400f, 0f);

    /// <summary>다른 패널이 Esc 처리를 양보할 때 쓴다.</summary>
    public static bool IsOpen { get; private set; }

    private string bottleId;
    private GameObject copy;
    private Camera copyCamera;
    private string notice;
    private bool deleteAsked;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        shelfButton.onClick.AddListener(OnShelfClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    private void OnEnable() { BottleStore.Changed += Redraw; }

    private void OnDisable()
    {
        BottleStore.Changed -= Redraw;
        if (IsOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    /// <summary>보관된 병(예: bottle1#4)을 자세히 본다. 열 수 없으면(병 프리팹을 못 찾음 등) 이유를 돌려준다. 열렸으면 null.</summary>
    public string Open(string archivedId)
    {
        if (IsOpen) return null;
        BottleRecord record = BottleStore.Get(archivedId);
        GameObject prefab = record.completed ? PrefabOf(record.typeId) : null;
        if (prefab == null) return "이 병은 자세히 볼 수 없어요";

        bottleId = archivedId;
        notice = null;
        deleteAsked = false;
        if (deleteLabel != null) deleteLabel.text = "병 지우기";

        // 병 종류의 프리팹을 복사한다. Instantiate 직전에 번호를 넣어 두면 그 병의 기록으로 병 속이 되살아난다.
        BottleContents.SpawnId = archivedId;
        copy = Instantiate(prefab, viewSpot, Quaternion.identity);
        BottleContents.SpawnId = null;
        copy.name = "DetailBottle";

        copyCamera = copy.GetComponent<BottleContents>().ViewCamera;
        if (copyCamera != null) copyCamera.enabled = true;
        dragRotate.SetTarget(copy.GetComponent<BottleViewer>());

        IsOpen = true;
        root.SetActive(true);
        UIFocus.Open();
        Redraw();
        return null;
    }

    /// <summary>병 종류 번호(예: bottle1)의 병 프리팹. 없으면 null.</summary>
    private GameObject PrefabOf(string typeId)
    {
        if (bottlePrefabs == null || string.IsNullOrEmpty(typeId)) return null;
        foreach (GameObject prefab in bottlePrefabs)
        {
            BottleContents contents = prefab != null ? prefab.GetComponent<BottleContents>() : null;
            if (contents != null && contents.BottleId == typeId) return prefab;
        }
        return null;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        dragRotate.SetTarget(null);
        if (copyCamera != null) copyCamera.enabled = false;
        copyCamera = null;
        if (copy != null) { copy.SetActive(false); Destroy(copy); copy = null; }

        root.SetActive(false);
        UIFocus.Close();
    }

    private void OnShelfClicked()
    {
        if (BottleStore.IsDisplayed(bottleId))
        {
            BottleStore.Undisplay(bottleId);
            notice = null;
        }
        else
        {
            notice = BottleStore.TryDisplay(bottleId) ? null : "선반이 가득 찼어요 (병을 더 만들면 칸이 늘어요)";
        }
        Redraw();
    }

    /// <summary>병 지우기: 누르면 바로 병을 지운다(선반에 있었으면 선반에서도 사라진다). 되돌릴 수 없다.</summary>
    private void OnDeleteClicked()
    {
        string name = nameLabel != null ? nameLabel.text : "병";
        string id = bottleId;
        Close();
        BottleStore.Remove(id);
        GameAudio.Play(GameAudio.Sfx.Paper, 0.8f);
        if (GameRoot.Instance != null)
        {
            if (GameRoot.Instance.Player != null)
            {
                Interactor who = GameRoot.Instance.Player.GetComponentInChildren<Interactor>();
                if (who != null) who.ShowMessage(name + "을(를) 지웠어요");
            }
            GameRoot.Instance.SaveNow();
        }
    }

    private void Redraw()
    {
        if (!IsOpen) return;

        BottleRecord record = BottleStore.Get(bottleId);
        GameObject prefab = PrefabOf(record.typeId);
        BottleContents type = prefab != null ? prefab.GetComponent<BottleContents>() : null;
        string typeName = type != null ? type.DisplayName : "병";
        bool shown = BottleStore.IsDisplayed(bottleId);

        nameLabel.text = BottleStore.NameOf(bottleId, typeName);
        string info = typeName + " #" + BottleStore.ArchiveNumber(bottleId) + "  |  재료 " + record.items.Count + "개  |  " + (shown ? "선반에 전시 중" : "선반에 없음");
        infoLabel.text = string.IsNullOrEmpty(notice) ? info : info + "\n" + notice;
        shelfLabel.text = shown ? "선반에서 내리기" : "선반에 올리기";
    }
}
