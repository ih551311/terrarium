using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 이 씬(섬)의 지도 정보. 눈 섬처럼 원래 섬이 아닌 씬에 하나 두면, 지도(M)와 작은 지도(N)가 이 그림과 범위를 쓴다.
/// 지도 그림은 섬 만들기 메뉴가 같이 만든다. 원래 섬(Island)은 이 오브젝트가 없어도 원래 지도를 쓴다.
/// "돌아가기" 버튼은 homeSpawnId 자리(눈 섬은 선착장 앞)로 보내 준다.
/// </summary>
public class SceneMapInfo : MonoBehaviour
{
    public Texture2D image;
    [Tooltip("지도 그림 가운데의 세계 좌표 (x, z)")]
    public Vector2 center;
    [Tooltip("지도 한 변의 절반(미터)")]
    public float halfExtent = 130f;
    [Tooltip("지도의 돌아가기 버튼이 보내 주는 도착 지점(SpawnPoint)의 id")]
    public string homeSpawnId = "FromDock";
    public string homeLabel = "선착장으로 돌아가기";

    private static SceneMapInfo cached;
    private static string cachedScene;

    /// <summary>지금 씬의 지도 정보. 없으면(원래 섬) null.</summary>
    public static SceneMapInfo Current
    {
        get
        {
            string scene = SceneManager.GetActiveScene().name;
            if (cachedScene != scene)
            {
                cachedScene = scene;
                cached = FindFirstObjectByType<SceneMapInfo>();
            }
            return cached;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cached = null;
        cachedScene = null;
    }
}
