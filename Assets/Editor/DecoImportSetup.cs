using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Models/Deco 의 장식 모델(.obj)을 게임에 맞게 가져오기 설정한다.
///  - 팔레트 그림(palette.png): 점 필터, 압축 없음, 크기 그대로 (색이 번지지 않게)
///  - 재질 LowpolyPalette(URP Lit): 모든 모델에 연결
///  - 재질 JarGlass(URP Lit, 투명): 병 유리 모델(jar_*, 뚜껑 jar_*_cork 제외)에만 연결
///  - 모델: 법선 가져오기, 카메라/조명 가져오지 않음
/// 메뉴: 유리병 정원 > 장식 모델 가져오기 설정. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class DecoImportSetup
{
    private const string Folder = "Assets/Models/Deco";
    private const string PalettePath = Folder + "/palette.png";
    private const string PaletteMaterialPath = Folder + "/LowpolyPalette.mat";
    private const string GlassMaterialPath = Folder + "/JarGlass.mat";
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
        tex.npotScale = TextureImporterNPOTScale.None; // 2의 거듭제곱으로 줄이면 색 경계가 번진다
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SaveAndReimport();

        // 2) 재질
        Texture2D palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        Material paletteMaterial = GetOrCreateMaterial(PaletteMaterialPath);
        paletteMaterial.SetTexture("_BaseMap", palette);
        paletteMaterial.SetColor("_BaseColor", Color.white);
        paletteMaterial.SetFloat("_Smoothness", 0f);
        paletteMaterial.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(paletteMaterial);

        // 병 유리: 투명(Transparent), 알파 0.25, 반짝임 높게. 팔레트 그림은 쓰지 않는다.
        Material glass = GetOrCreateMaterial(GlassMaterialPath);
        glass.SetColor("_BaseColor", new Color(0.88f, 0.96f, 1f, 0.2f)); // 팔레트의 glass 색(#CEEAF2)보다 밝게. 알파는 병 속 재료가 잘 보이도록 0.2 (0.3이면 앞면 두 겹이 뿌옇게 가렸다)
        glass.SetFloat("_Surface", 1f);    // Transparent
        glass.SetFloat("_Blend", 0f);      // Alpha
        glass.SetFloat("_SrcBlend", 5f);   // SrcAlpha
        glass.SetFloat("_DstBlend", 10f);  // OneMinusSrcAlpha
        glass.SetFloat("_ZWrite", 0f);
        glass.SetFloat("_Smoothness", 0.9f);
        glass.SetOverrideTag("RenderType", "Transparent");
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glass.renderQueue = 3000;
        EditorUtility.SetDirty(glass);

        AssetDatabase.SaveAssets();
        paletteMaterial = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
        glass = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);

        // 3) 모델
        int count = 0, glassCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".obj")) continue;

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            // 병 유리만 투명 재질. 뚜껑(_cork)과 모래 바닥(_floor)은 팔레트 재질을 쓴다.
            bool isGlass = name.StartsWith("jar_") && !name.EndsWith("_cork") && !name.EndsWith("_floor");

            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importNormals = ModelImporterNormals.Import;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = true; // 병 안쪽 단면을 읽어 흙 모양을 만들 때 필요하다
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), SourceMaterialName),
                              isGlass ? glass : paletteMaterial);
            importer.SaveAndReimport();
            count++;
            if (isGlass) glassCount++;
        }

        AssetDatabase.SaveAssets();
        return "장식 모델 " + count + "개 설정 완료 (유리 " + glassCount + "개)";
    }

    private static Material GetOrCreateMaterial(string path)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
