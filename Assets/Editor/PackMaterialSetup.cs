using UnityEditor;
using UnityEngine;

/// <summary>
/// 에셋 팩의 기존(Built-in) 표준 셰이더 재질을 URP Lit으로 바꾼다. (바꾸지 않으면 URP에서 분홍색으로 보인다)
/// 색 그림(_MainTex)과 색, 컷아웃(알파 잘라내기), 투명을 URP Lit 설정으로 옮긴다.
/// 이미 URP나 직접 만든 셰이더(물, 바람 등)를 쓰는 재질은 건드리지 않는다.
/// 메뉴: 유리병 정원 > 에셋 팩 재질을 URP로 바꾸기. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class PackMaterialSetup
{
    private static readonly string[] Folders =
    {
        "Assets/ThirdParty/LowPolyNaturePack/Materials",
        "Assets/ThirdParty/Donthrukat",
    };

    [MenuItem("유리병 정원/에셋 팩 재질을 URP로 바꾸기")]
    public static string Build()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        int converted = 0, skipped = 0;
        var log = new System.Text.StringBuilder();

        foreach (string guid in AssetDatabase.FindAssets("t:Material", Folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null) continue;

            string shaderName = m.shader.name;
            bool builtIn = shaderName == "Standard" || shaderName == "Standard (Specular setup)" || shaderName.StartsWith("Legacy Shaders/");
            if (!builtIn) { skipped++; continue; }

            // 바꾸기 전에 값을 읽어 둔다 (셰이더를 바꾸면 이전 속성 이름이 사라진다)
            Texture mainTex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            float mode = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f;       // 0 불투명, 1 컷아웃, 2 페이드, 3 투명
            float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;

            m.shader = lit;
            m.SetTexture("_BaseMap", mainTex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0f);   // 로우폴리는 반짝임 없이
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_SpecularHighlights", 0f);
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");

            if (mode == 1f) // 컷아웃: 알파가 기준보다 작으면 자른다 (잎, 풀)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", cutoff);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = 2450;
                m.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else if (mode >= 2f) // 투명
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", 5f);
                m.SetFloat("_DstBlend", 10f);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                m.SetOverrideTag("RenderType", "Transparent");
            }

            EditorUtility.SetDirty(m);
            converted++;
            log.Append(m.name + "(" + shaderName + ", 모드 " + mode + ") ");
        }

        AssetDatabase.SaveAssets();
        return "URP로 바꾼 재질 " + converted + "개, 건너뜀 " + skipped + "개: " + log;
    }
}
