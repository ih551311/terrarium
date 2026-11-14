using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 자세히 보기. 선반의 병을 바라보고 E를 누르거나, Tab 가방의 병 탭에서 병을 누르면 열린다.
/// 완성한 병을 크게 보여 주고, 마우스로 끌면 돌려 볼 수 있다. (T: 처음 방향) 이름과 종류, 선반에 놓였는지도 보인다.
/// 병은 작업대의 병과 같은 병 프리팹을 복사해 눈에 안 보이는 먼 곳에 놓고, 병 카메라가 RenderTexture에 그려서
/// 패널 안 RawImage로 보여 준다. (병 꾸미기 패널이 쓰는 것과 같은 그림 판이고, 두 패널이 동시에 열리지는 않는다)
/// 병 프리팹은 집 안의 작업대(BottleStation)에서 찾으므로, 집 안에서만 열린다.
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

    [Tooltip("병을 복사해 놓는 자리 (병 카메라는 병 레이어만 그리므로 눈에 띄지 않는 먼 곳이면 된다)")]
    [SerializeField] private Vector3 viewSpot = new Vector3(0f, -400f, 0f);

    /// <summary>다른 패널이 Esc 처리를 양보할 때 쓴다.</summary>
    public static bool IsOpen { get; private set; }

    private string bottleId;
    private BottleStation station;
    private GameObject copy;
    private Camera copyCamera;
    private string notice;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        shelfButton.onClick.AddListener(OnShelfClicked);
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
        station = FindFirstObjectByType<BottleStation>(FindObjectsInactive.Include);
        if (station == null) return "병은 집 안에서 자세히 볼 수 있어요";

        BottleRecord record = BottleStore.Get(archivedId);
        BottleContents type = station.GetById(record.typeId);
        if (!record.completed || type == null || type.Prefab == null) return "이 병은 자세히 볼 수 없어요";

        bottleId = archivedId;
        notice = null;

        // 작업대와 같은 병을 복사한다. Instantiate 직전에 번호를 넣어 두면 그 병의 기록으로 병 속이 되살아난다.
        BottleContents.SpawnId = archivedId;
        copy = Instantiate(type.Prefab, viewSpot, Quaternion.identity);
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

    private void Redraw()
    {
        if (!IsOpen || station == null) return;

        BottleRecord record = BottleStore.Get(bottleId);
        BottleContents type = station.GetById(record.typeId);
        string typeName = type != null ? type.DisplayName : "병";
        bool shown = BottleStore.IsDisplayed(bottleId);

        nameLabel.text = BottleStore.NameOf(bottleId, typeName);
        string info = typeName + " #" + BottleStore.ArchiveNumber(bottleId) + "  |  재료 " + record.items.Count + "개  |  " + (shown ? "선반에 전시 중" : "선반에 없음");
        infoLabel.text = string.IsNullOrEmpty(notice) ? info : info + "\n" + notice;
        shelfLabel.text = shown ? "선반에서 내리기" : "선반에 올리기";
    }
}
