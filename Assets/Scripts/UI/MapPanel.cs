using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 섬 지도 패널. 섬에서 M 키로 열고 닫는다. 섬을 위에서 찍은 그림 위에 내 위치(빨간 표시)와 집을 보여 주고,
/// "집으로 돌아가기" 버튼으로 오두막 앞으로 바로 간다. (집 안에서는 열리지 않는다)
/// 지도 그림은 메뉴 "유리병 정원 > 섬 지도 만들기"가 만들고, 이 패널은 UISetup이 만든다.
/// </summary>
public class MapPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button homeButton;

    [Header("지도 그림 위의 표시")]
    [Tooltip("지도 그림(RawImage)의 RectTransform. 표시는 이 안에서 움직인다.")]
    [SerializeField] private RectTransform mapRect;
    [Tooltip("내 위치. 바라보는 방향으로 돈다.")]
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private RectTransform hutMarker;

    [Header("지도가 덮는 땅")]
    [Tooltip("지도 가운데의 세계 좌표 (x, z)")]
    [SerializeField] private Vector2 mapCenter = new Vector2(0f, 30f);
    [Tooltip("지도 한 변의 절반(미터)")]
    [SerializeField] private float mapHalfExtent = 175f;
    [SerializeField] private string islandSceneName = "Island";

    [Tooltip("\"바다에는 갈 수 없어요\" 같은 안내를 잠깐 보여 주는 글자")]
    [SerializeField] private TMPro.TMP_Text messageText;
    [SerializeField] private float messageSeconds = 2.5f;

    private bool isOpen;
    private float messageUntil;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        homeButton.onClick.AddListener(GoHome);
    }

    private void OnDisable()
    {
        if (isOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.mKey.wasPressedThisFrame)
        {
            if (isOpen) Close();
            else if (!UIFocus.IsOpen && SceneManager.GetActiveScene().name == islandSceneName) Open();
        }
        else if (isOpen && !InventoryPanel.IsOpen && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }

        if (isOpen) UpdateMarkers();
        if (messageText != null && messageUntil > 0f && Time.time > messageUntil)
        {
            messageText.text = string.Empty;
            messageUntil = 0f;
        }
    }

    /// <summary>
    /// 지도 위를 클릭했을 때 호출된다. 클릭한 자리의 세계 좌표를 구해 그 자리로 이동한다.
    /// 바다이거나 나무와 바위로 막힌 곳이면 이동하지 않고 안내만 보여 준다.
    /// </summary>
    public void OnMapClicked(Vector2 screenPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screenPosition, null, out Vector2 local)) return;

        Rect r = mapRect.rect;
        float u = (local.x - r.xMin) / r.width;
        float v = (local.y - r.yMin) / r.height;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        Vector2 world = new Vector2(mapCenter.x + (u - 0.5f) * 2f * mapHalfExtent, mapCenter.y + (v - 0.5f) * 2f * mapHalfExtent);
        if (GameRoot.Instance != null && GameRoot.Instance.TeleportTo(world))
        {
            Close();
        }
        else if (messageText != null)
        {
            messageText.text = "여기로는 갈 수 없어요 (바다이거나 막힌 곳)";
            messageUntil = Time.time + messageSeconds;
        }
    }

    private void Open()
    {
        isOpen = true;
        if (messageText != null) messageText.text = string.Empty;
        root.SetActive(true);
        UIFocus.Open();
        UpdateMarkers();
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        root.SetActive(false);
        UIFocus.Close();
    }

    private void GoHome()
    {
        if (GameRoot.Instance != null) GameRoot.Instance.GoHome();
        Close();
    }

    private void UpdateMarkers()
    {
        if (GameRoot.Instance == null) return;

        Transform player = GameRoot.Instance.Player;
        playerMarker.anchoredPosition = ToMap(new Vector2(player.position.x, player.position.z));
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y); // 위쪽이 북쪽(+z)

        // 집 표시는 "집 앞 도착 지점"이 있는 곳에 둔다.
        hutMarker.gameObject.SetActive(false);
        foreach (SpawnPoint spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (spawn.Id != "FromHut") continue;
            hutMarker.anchoredPosition = ToMap(new Vector2(spawn.transform.position.x, spawn.transform.position.z + 6f));
            hutMarker.gameObject.SetActive(true);
            break;
        }
    }

    /// <summary>세계 좌표(x, z)를 지도 그림 위의 위치로 바꾼다. 지도의 왼쪽 아래가 기준이다.</summary>
    private Vector2 ToMap(Vector2 world)
    {
        Vector2 size = mapRect.rect.size;
        float u = (world.x - mapCenter.x) / (mapHalfExtent * 2f) + 0.5f;
        float v = (world.y - mapCenter.y) / (mapHalfExtent * 2f) + 0.5f;
        return new Vector2(Mathf.Clamp01(u) * size.x, Mathf.Clamp01(v) * size.y);
    }
}
