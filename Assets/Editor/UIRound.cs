using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면의 네모난 바탕(패널, 버튼, 칸, 안내 막대)을 모서리가 둥근 그림으로 바꾼다.
/// 둥근 사각형 그림(Assets/UI/Common/rounded.png)은 코드로 한 번 그려 두고, 가장자리만 늘어나는 9칸 그림(Sliced)으로 쓴다.
/// 화면을 만드는 도구(UISetup, UIBake)가 끝에 부른다. 그림이 이미 있는 Image와 화면 전체를 덮는 막은 건드리지 않는다.
/// </summary>
public static class UIRound
{
    private const string Path = "Assets/UI/Common/rounded.png";
    private const int Size = 64;
    private const float Radius = 18f;
    private const float Border = 20f;

    public static Sprite GetSprite()
    {
        if (!File.Exists(Path))
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - Size / 2f) - Size / 2f + Radius;
                    float qy = Mathf.Abs(y + 0.5f - Size / 2f) - Size / 2f + Radius;
                    float d = Mathf.Min(Mathf.Max(qx, qy), 0f) + new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude - Radius;
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                }
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(Path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Path);
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder.x != Border))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(Border, Border, Border, Border);
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(Path);
    }

    /// <summary>root 아래의 그림 없는 네모 Image를 둥근 모서리로 바꾼다. 바꾼 개수를 돌려준다.</summary>
    public static int Apply(GameObject root)
    {
        Sprite sprite = GetSprite();
        if (sprite == null) return 0;
        int n = 0;
        foreach (Image img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite != null) continue;
            if (img.type != Image.Type.Simple && img.type != Image.Type.Sliced) continue;
            if (img.name == "Backdrop" || img.name == "Curtain") continue;
            if (CoversParent(img.rectTransform)) continue;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            EditorUtility.SetDirty(img);
            n++;
        }
        return n;
    }

    private static bool CoversParent(RectTransform r)
    {
        return r.anchorMin == Vector2.zero && r.anchorMax == Vector2.one && r.offsetMin.sqrMagnitude < 1f && r.offsetMax.sqrMagnitude < 1f;
    }
}