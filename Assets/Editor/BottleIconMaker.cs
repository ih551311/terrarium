using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 병 만들기 화면의 카드에 넣을 병 예시 그림을 만든다: Assets/Data/BottleIcons/bottle_(둥근 병 등).png
/// 병 프리팹을 처음 방향(입구가 카메라 쪽으로 조금 기울어진 모습)으로 렌더해서 찍는다. 병 모양을 바꿨으면 이 메뉴를 다시 누른다.
/// 메뉴: 유리병 정원 > 병 그림 만들기. (UI 만들기보다 먼저 한다)
/// </summary>
public static class BottleIconMaker
{
    public const string Folder = "Assets/Data/BottleIcons";
    private const int Size = 384;
    private static readonly string[] Ids = { "round", "tall", "flask", "side" };

    [MenuItem("유리병 정원/병 그림 만들기")]
    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        int made = 0;

        for (int i = 0; i < Ids.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bottles/Bottle_" + Ids[i] + ".prefab");
            if (prefab == null) continue;

            // 다른 병과 겹치지 않게 멀리 떨어진 곳에 임시로 놓고 찍는다
            GameObject copy = (GameObject)Object.Instantiate(prefab, new Vector3(5000f + i * 50f, 0f, 0f), Quaternion.identity);
            copy.GetComponent<BottleViewer>().ResetOrientation();

            Camera cam = copy.GetComponent<BottleContents>().ViewCamera;
            cam.targetTexture = rt;
            cam.enabled = true;
            cam.fieldOfView = 68f; // 병이 잘리지 않게 병 카메라(60)보다 조금 넓게
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string path = PathOf(Ids[i]);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(copy);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            made++;
        }

        Object.DestroyImmediate(rt);
        return "병 그림 " + made + "장을 만들었어요: " + Folder;
    }

    private static string PathOf(string id)
    {
        return Folder + "/bottle_" + id + ".png";
    }

    /// <summary>번호 순서(둥근 병, 긴 병, 넓은 병, 누운 병)의 병 그림. 없으면 null.</summary>
    public static Sprite Load(int index)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(Ids[index]));
    }
}
