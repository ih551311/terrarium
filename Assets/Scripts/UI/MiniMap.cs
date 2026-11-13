using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위에 항상 떠 있는 작은 지도. 내 주변만 크게 보여 주고 내 위치(빨간 표시)는 가운데 근처에 있다.
/// 섬에서만 보이고(집 안에서는 숨김), 패널이 열려 있으면 숨긴다. N 키로 켜고 끈다.
/// 지도 그림은 큰 지도(MapPanel)와 같은 것을 쓴다. 모양은 UISetup이 만든다.
/// </summary>
public class MiniMap : MonoBehaviour
{
    [Tooltip("켜고 끄는 작은 지도 전체")]
    [SerializeField] private GameObject root;
    [SerializeField] private RawImage image;
    [SerializeField] private RectTransform area;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private RectTransform hutMarker;

    [Header("지도가 덮는 땅 (큰 지도와 같게)")]
    [SerializeField] private Vector2 mapCenter = new Vector2(0f, 30f);
    [SerializeField] private float mapHalfExtent = 175f;
    [Tooltip("작은 지도 한 변이 보여 주는 땅의 길이(미터)")]
    [SerializeField] private float viewMeters = 130f;
    [SerializeField] private string islandSceneName = "Island";

    private bool hiddenByUser;
    private Vector2 hutWorld;
    private bool hasHut;
    private string hutSearchedScene;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.nKey.wasPressedThisFrame && !UIFocus.IsOpen) hiddenByUser = !hiddenByUser;

        bool onIsland = SceneManager.GetActiveScene().name == islandSceneName;
        bool show = onIsland && !hiddenByUser && !UIFocus.IsOpen && GameRoot.Instance != null;
        if (root.activeSelf != show) root.SetActive(show);
        if (!show) return;

        FindHut();

        Transform player = GameRoot.Instance.Player;
        float full = mapHalfExtent * 2f;
        float fraction = viewMeters / full;
        Vector2 uv = new Vector2((player.position.x - mapCenter.x) / full + 0.5f, (player.position.z - mapCenter.y) / full + 0.5f);

        // 보이는 창이 지도 그림 밖으로 나가지 않게 한다
        Vector2 origin = new Vector2(Mathf.Clamp(uv.x - fraction * 0.5f, 0f, 1f - fraction), Mathf.Clamp(uv.y - fraction * 0.5f, 0f, 1f - fraction));
        image.uvRect = new Rect(origin.x, origin.y, fraction, fraction);

        Vector2 size = area.rect.size;
        playerMarker.anchoredPosition = ((uv - origin) / fraction) * size;
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);

        if (hasHut)
        {
            Vector2 hutUv = new Vector2((hutWorld.x - mapCenter.x) / full + 0.5f, (hutWorld.y - mapCenter.y) / full + 0.5f);
            Vector2 local = (hutUv - origin) / fraction;
            bool inside = local.x > 0f && local.x < 1f && local.y > 0f && local.y < 1f;
            hutMarker.gameObject.SetActive(inside);
            if (inside) hutMarker.anchoredPosition = local * size;
        }
    }

    /// <summary>집 앞 도착 지점(FromHut)의 위치를 한 번 찾아 둔다. (씬이 바뀌면 다시 찾는다)</summary>
    private void FindHut()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (hutSearchedScene == scene) return;
        hutSearchedScene = scene;
        hasHut = false;
        foreach (SpawnPoint spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (spawn.Id != "FromHut") continue;
            hutWorld = new Vector2(spawn.transform.position.x, spawn.transform.position.z + 6f);
            hasHut = true;
            break;
        }
    }
}
