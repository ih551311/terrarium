using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬을 위에서 내려다본 그림(지도)을 만든다: Assets/Data/IslandMap.png.
/// 구름과 안개는 끄고 찍는다. 섬을 다시 만들었으면(메뉴 "섬 만들기") 이 메뉴도 다시 누른다.
/// 메뉴: 유리병 정원 > 섬 지도 만들기.
/// </summary>
public static class MapBake
{
    /// <summary>지도가 덮는 땅의 가운데(세계 x, z)</summary>
    public static readonly Vector2 Center = new Vector2(0f, 30f);

    /// <summary>지도 한 변의 절반(미터). 섬 반지름 150m에 바다 여유를 더했다.</summary>
    public const float HalfExtent = 175f;

    public const string ImagePath = "Assets/Data/IslandMap.png";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const int Size = 1024;

    [MenuItem("유리병 정원/섬 지도 만들기")]
    public static string Build()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);

        // 찍는 동안만 구름과 안개를 끈다
        GameObject clouds = GameObject.Find("Clouds");
        bool cloudsWere = clouds != null && clouds.activeSelf;
        if (clouds != null) clouds.SetActive(false);
        bool fogWas = RenderSettings.fog;
        RenderSettings.fog = false;

        GameObject camGo = new GameObject("MapBakeCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = HalfExtent;
        cam.transform.position = new Vector3(Center.x, 400f, Center.y);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 아래를 보고, 화면 위쪽이 북쪽(+z)
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 1000f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.72f, 0.85f);

        RenderTexture rt = new RenderTexture(Size, Size, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(ImagePath, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGo);
        if (clouds != null) clouds.SetActive(cloudsWere);
        RenderSettings.fog = fogWas;

        AssetDatabase.ImportAsset(ImagePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ImagePath);
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        return "섬 지도 만들기 완료: " + ImagePath;
    }
}
