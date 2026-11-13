using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Models/Deco 의 장식 모델(.obj)을 게임에 맞게 가져오기 설정한다.
///  - 팔레트 그림(palette.png): 점 필터, 압축 없음 (색이 번지지 않게)
///  - 재질 LowpolyPalette(URP Lit)를 만들고 모든 모델에 연결
///  - 모델: 법선 가져오기, 카메라/조명 가져오지 않음
/// 메뉴: 유리병 정원 > 장식 모델 가져오기 설정. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class DecoImportSetup
{
    private const string Folder = "Assets/Models/Deco";
    private const string PalettePath = Folder + "/palette.png";
    private const string MaterialPath = Folder + "/LowpolyPalette.mat";
    private const string SourceMaterialName = "lowpoly_palette"; // .mtl 안의 재질 이름

    [MenuItem("유리병 정원/장식 모델 가져오기 설정")]
    public static string Build()
    {
        AssetDatabase.Refresh();

        // 1) 팔레트 그림
        TextureImporter tex = (TextureImporter)AssetImporter.GetAtPath(PalettePath);
        if (tex == null) return "palette.png를 찾을 수 없어요: " + PalettePath;
        tex.filterMode = FilterMode.Point;
        tex.textureCompression = TextureImporterCompression.Uncompressed;
        tex.mipmapEnabled = false;
        tex.npotScale = TextureImporterNPOTScale.None; // 264x8 그대로 (2의 거듭제곱으로 줄이면 색 경계가 번진다)
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SaveAndReimport();

        // 2) 재질 (URP Lit, 반짝임 없음)
        Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.SetTexture("_BaseMap", palette);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        // 3) 모델 44개
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".obj")) continue;

            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importNormals = ModelImporterNormals.Import;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), SourceMaterialName), material);
            importer.SaveAndReimport();
            count++;
        }

        AssetDatabase.SaveAssets();
        return "장식 모델 " + count + "개 설정 완료";
    }
}
