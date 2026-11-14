using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 눈 섬에 쓰는 에셋 "Cozy Snowy Environment - Low Poly"(HobiSoLoved, Assets/HobiSoloved)를 게임(URP)에 맞게 준비한다.
/// 이 에셋의 재질은 기본(Standard) 셰이더라 URP에서는 분홍색으로 보이므로 URP Lit으로 바꾼다.
/// (유료 에셋이라 저장소에는 올리지 않는다. 바뀐 재질 파일도 에셋 폴더 안에 있어서 함께 올라가지 않는다)
/// 메뉴: 유리병 정원 > 눈 에셋 재질 바꾸기
/// </summary>
public static class SnowAssetSetup
{
    public const string Folder = "Assets/HobiSoloved";

    [MenuItem("유리병 정원/눈 에셋 재질 바꾸기")]
    public static string ConvertMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return "URP Lit 셰이더를 찾을 수 없어요.";
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder + "/Materials" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == lit) continue;

            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            m.shader = lit;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            n++;
        }
        AssetDatabase.SaveAssets();
        return "눈 에셋 재질 " + n + "개를 URP로 바꿨어요";
    }

    /// <summary>에셋의 프리팹들을 한 장에 줄 세워 찍는다. (모양과 크기를 보려고)</summary>
    public static string Contact(string outPath, string[] names)
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);
        Light light = new GameObject("L").AddComponent<Light>();
        light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50f, -30f, 0f); light.intensity = 1.1f;
        Camera cam = new GameObject("C").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.65f, 0.75f);
        cam.fieldOfView = 30f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1000f;
        int cell = 320, cols = 6, rows = (names.Length + cols - 1) / cols;
        var rt = new RenderTexture(cell, cell, 24); cam.targetTexture = rt;
        var sheet = new Texture2D(cols * cell, rows * cell, TextureFormat.RGB24, false);
        var report = new System.Text.StringBuilder();
        for (int i = 0; i < names.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Prefabs/" + names[i] + ".prefab");
            if (prefab == null) continue;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(500f, 0f, 0f);
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds; foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            report.Append(names[i]).Append(' ').Append(b.size.ToString("F1")).Append("; ");
            float dist = b.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            cam.transform.position = b.center + new Vector3(0.6f, 0.5f, -1f).normalized * dist;
            cam.transform.LookAt(b.center);
            cam.Render();
            RenderTexture.active = rt;
            var tile = new Texture2D(cell, cell, TextureFormat.RGB24, false);
            tile.ReadPixels(new Rect(0, 0, cell, cell), 0, 0); tile.Apply();
            sheet.SetPixels((i % cols) * cell, (rows - 1 - i / cols) * cell, cell, cell, tile.GetPixels());
            Object.DestroyImmediate(tile); Object.DestroyImmediate(go);
        }
        RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt);
        File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        EditorSceneManager.OpenScene("Assets/Scenes/Island.unity");
        return report.ToString();
    }
}
