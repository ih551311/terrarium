using UnityEngine;

/// <summary>
/// 눈 섬의 밤하늘에 걸리는 오로라. 북쪽 하늘에 빛의 커튼 두 겹이 걸려 일렁이고, 밤이 깊어질수록 짙어지며 아침에는 사라진다.
/// 하늘 위에 아주 멀리 그린 띠 메쉬(반지름 600m)라서 카메라를 따라다니고(가까이 가도 닿지 않는다), 앞의 나무와 산은 가린다.
/// 눈 섬 씬에 하나 둔다(SnowIslandSetup이 만든다). 재질은 Resources/AuroraMat (셰이더 유리병 정원/Aurora).
/// </summary>
public class AuroraSky : MonoBehaviour
{
    [Tooltip("북쪽(+z)에서 얼마나 돌아가 있는가(도). 커튼의 가운데 방향")]
    [SerializeField] private float centerAzimuth = 8f;
    [Tooltip("밤의 세기 (1이 기본)")]
    [SerializeField] private float strength = 1.1f;

    private const float Radius = 600f;

    private Material material;
    private MeshRenderer[] renderers;
    private Transform cam;
    private float shown;

    private void Start()
    {
        material = Resources.Load<Material>("AuroraMat");
        if (material == null) { Debug.LogWarning("오로라 재질(Resources/AuroraMat)을 찾지 못했어요."); enabled = false; return; }

        // 커튼 두 겹: 가깝고 낮은 것과, 멀고 높은 것 (색 무늬가 달라서 겹치면 깊이가 생긴다)
        renderers = new[]
        {
            BuildRibbon("AuroraCurtainA", centerAzimuth, 150f, 11f, 34f, 0f, 1.0f),
            BuildRibbon("AuroraCurtainB", centerAzimuth - 22f, 120f, 18f, 30f, 1.7f, 0.65f),
        };
    }

    private MeshRenderer BuildRibbon(string name, float azimuth, float span, float baseElevation, float height, float phase, float seed)
    {
        const int columns = 96;
        var vertices = new Vector3[(columns + 1) * 2];
        var uvs = new Vector2[(columns + 1) * 2];
        var triangles = new int[columns * 6];
        for (int i = 0; i <= columns; i++)
        {
            float u = i / (float)columns;
            float az = (azimuth + (u - 0.5f) * span) * Mathf.Deg2Rad;
            // 커튼의 아래 선이 구불구불하고, 높이도 군데군데 다르다
            float lowEl = baseElevation + 5f * Mathf.Sin(u * 7f + phase) + 2.5f * Mathf.Sin(u * 17f + phase * 2f);
            float hi = height + 9f * Mathf.Sin(u * 5f + phase + 1f) + 4f * Mathf.Sin(u * 13f);
            for (int row = 0; row < 2; row++)
            {
                float el = (lowEl + row * Mathf.Max(8f, hi)) * Mathf.Deg2Rad;
                float c = Mathf.Cos(el);
                vertices[i * 2 + row] = new Vector3(Radius * c * Mathf.Sin(az), Radius * Mathf.Sin(el), Radius * c * Mathf.Cos(az));
                uvs[i * 2 + row] = new Vector2(u, row);
            }
        }
        for (int i = 0; i < columns; i++)
        {
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2; triangles[t + 4] = v + 1; triangles[t + 5] = v + 3;
        }
        var mesh = new Mesh { name = name };
        mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * Radius * 3f); // 카메라가 어디를 보든 그려지게

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        Material m = new Material(material);
        m.SetFloat("_Seed", seed * 10f);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.enabled = false;
        return r;
    }

    private void LateUpdate()
    {
        if (renderers == null) return;
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam != null) transform.position = cam.position; // 하늘은 카메라를 따라다닌다

        // 밤이 되면 서서히 나타난다 (시간 0.74에서 시작해 0.86에서 가장 짙다)
        float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.88f, GameState.TimeOfDay));
        shown = Mathf.MoveTowards(shown, night, Time.deltaTime * 0.25f);
        foreach (MeshRenderer r in renderers)
        {
            r.enabled = shown > 0.01f;
            if (r.enabled) r.sharedMaterial.SetFloat("_Intensity", shown * strength);
        }
    }
}
