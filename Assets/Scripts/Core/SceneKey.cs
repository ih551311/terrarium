using UnityEngine.SceneManagement;

/// <summary>
/// 주운 자리와 캔 자리를 기억하는 이름(id)은 자리의 좌표로 만든다. 섬이 둘이 되면서 다른 섬의 같은 좌표와 겹치지 않도록,
/// 눈 섬 같은 다른 씬의 자리에는 씬 이름을 앞에 붙인다. (원래 섬은 그대로라서 예전 저장도 그대로 읽힌다)
/// </summary>
public static class SceneKey
{
    public static string Prefix()
    {
        string scene = SceneManager.GetActiveScene().name;
        return scene == "Island" || string.IsNullOrEmpty(scene) ? string.Empty : scene + "_";
    }
}
