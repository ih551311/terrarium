using UnityEngine;

/// <summary>
/// 집 안 영역. 플레이어가 이 상자들 안에 있으면 "집 안"이다. (예전에는 집 안이 따로 떨어진 씬이라 지형이 없는 것으로 알았다)
/// 집 모양은 현관 앞방이 앞으로 튀어나온 T자라서 상자 여러 개로 나타낸다.
/// 집 안에서는 비와 눈이 숨고(WeatherEffects), 바깥 소리 대신 방 소리가 나고(GameAudio), 발소리가 나무 바닥 소리가 되고(FootstepPlayer),
/// 환경광이 따뜻하고 어둡게 바뀐다 (낮 하늘빛이 지붕 아래까지 들어오지 않게). 들어가고 나올 때 환경광은 부드럽게 바뀐다.
/// 상자는 이 오브젝트(집 안 뿌리)의 좌표계로 centers, sizes를 준다. InteriorSetup이 만들어서 붙인다.
/// </summary>
[DefaultExecutionOrder(500)] // SkyController가 환경광을 정한 다음에 덮어쓴다
public class IndoorZone : MonoBehaviour
{
    [SerializeField] private Vector3[] centers = { new Vector3(0f, 1.6f, 0f) };
    [SerializeField] private Vector3[] sizes = { new Vector3(10f, 3.4f, 8f) };

    [Header("집 안 환경광 (하늘, 수평선, 바닥 세 색)")]
    [SerializeField] private Color indoorSky = new Color(0.50f, 0.45f, 0.40f);
    [SerializeField] private Color indoorEquator = new Color(0.46f, 0.41f, 0.36f);
    [SerializeField] private Color indoorGround = new Color(0.34f, 0.29f, 0.25f);

    private static IndoorZone current;
    private float factor;   // 0 바깥 ~ 1 집 안 (부드럽게 바뀐다)

    /// <summary>지금 집 안에 있는가</summary>
    public static bool Inside { get; private set; }

    /// <summary>0(바깥) ~ 1(집 안). 문턱을 넘는 동안 부드럽게 바뀐다.</summary>
    public static float Factor => current != null ? current.factor : 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current = null;
        Inside = false;
    }

    private void OnEnable() { current = this; }

    private void OnDisable()
    {
        if (current == this) { current = null; Inside = false; }
    }

    /// <summary>세계 좌표의 한 점이 집 안인가</summary>
    public bool Contains(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        for (int i = 0; i < centers.Length && i < sizes.Length; i++)
        {
            Vector3 d = local - centers[i];
            if (Mathf.Abs(d.x) <= sizes[i].x * 0.5f && Mathf.Abs(d.y) <= sizes[i].y * 0.5f && Mathf.Abs(d.z) <= sizes[i].z * 0.5f) return true;
        }
        return false;
    }

    private void Update()
    {
        Transform player = GameRoot.Instance != null ? GameRoot.Instance.Player : null;
        Inside = player != null && Contains(player.position + Vector3.up * 0.9f);
        factor = Mathf.MoveTowards(factor, Inside ? 1f : 0f, Time.deltaTime * 1.5f);
    }

    private void LateUpdate()
    {
        if (factor <= 0.001f) return;
        float k = Mathf.SmoothStep(0f, 1f, factor);
        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, indoorSky, k);
        RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, indoorEquator, k);
        RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, indoorGround, k);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        for (int i = 0; i < centers.Length && i < sizes.Length; i++) Gizmos.DrawWireCube(centers[i], sizes[i]);
    }
}
