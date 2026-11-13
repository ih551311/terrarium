using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 섬 지도 그림(Assets/Data/IslandMap.png)을 만든다.
/// 카메라로 위에서 찍지 않고, 섬의 지형 계산(IslandSetup)으로 직접 칠한다. 그래서 바다와 모래사장, 풀밭, 숲,
/// 꽃 언덕이 또렷하게 나뉘고, 길은 그리지 않는다. 섬 모양을 바꾸면 이 메뉴를 다시 누른다.
/// 메뉴: 유리병 정원 > 섬 지도 만들기.
/// </summary>
public static class MapBake
{
    /// <summary>지도가 덮는 땅의 가운데(세계 x, z)</summary>
    public static readonly Vector2 Center = new Vector2(0f, 30f);

    /// <summary>지도 한 변의 절반(미터). 섬 반지름 150m에 바다 여유를 더했다.</summary>
    public const float HalfExtent = 175f;

    public const string ImagePath = "Assets/Data/IslandMap.png";
    private const int Size = 1024;

    [MenuItem("유리병 정원/섬 지도 만들기")]
    public static string Build()
    {
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            float z = Center.y + ((y + 0.5f) / Size - 0.5f) * 2f * HalfExtent; // 위쪽이 북쪽(+z)
            for (int x = 0; x < Size; x++)
            {
                float wx = Center.x + ((x + 0.5f) / Size - 0.5f) * 2f * HalfExtent;
                pixels[y * Size + x] = IslandSetup.MapColorAt(wx, z);
            }
        }

        var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ImagePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

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
