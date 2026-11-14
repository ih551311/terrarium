using System.Collections.Generic;
using UnityEngine;

/// <summary>재료를 놓을 자리를 찾은 결과</summary>
public enum PlacementResult
{
    Ok,       // 놓을 수 있다 (병 밖으로 나가면 안쪽으로 밀어 넣은 자리)
    Outside,  // 마우스가 병 안 바닥에서 너무 멀다
    TooBig    // 이 병에는 어디에도 들어가지 않는다 (너무 크거나 높다)
}

/// <summary>
/// 병 하나의 속(바닥재와, 그 표면 위에 놓인 재료들)을 관리한다.
/// 병 뿌리 오브젝트(Bottle)에 붙인다. 놓은 재료는 병의 자식이라 병과 함께 돈다.
/// 규칙: 재료는 바닥재 표면 위에만 놓이고, 물리 효과는 없고, 서로 겹쳐도 되고, 병 하나에 12개까지다.
///       재료의 모든 부분이 병 안쪽 안에 있어야 한다(병 밖으로 나가지 않는다).
/// 완성하면 뚜껑이 닫히고 더는 고칠 수 없다.
///
/// 병 안쪽 모양은 "축을 기준으로 한 안쪽 반지름" 표(profile)로 알고 있다.
/// 세워 둔 병은 위(y)가 축이고, 누운 병은 가로(x)가 축이다. 표는 JarSetup이 병 모델에서 읽어 채운다.
/// </summary>
public class BottleContents : MonoBehaviour
{
    [Tooltip("이 병을 구별하는 이름 (병마다 달라야 한다). 예: bottle0")]
    [SerializeField] private string bottleId = "bottle0";

    [Tooltip("바닥재 표면의 높이 (병 기준 위쪽 방향 거리)")]
    [SerializeField] private float surfaceLocalY = 0.07f;

    [SerializeField] private int maxItems = 12;

    [Tooltip("병 화면을 가장 크게 확대했을 때 바라보는 곳(병 기준). 흙 위를 크게 볼 수 있는 곳.")]
    [SerializeField] private Vector3 zoomLookPoint = new Vector3(0f, 0.06f, 0f);

    [Header("병 안쪽 모양 (JarSetup이 채운다)")]
    [Tooltip("켜면 병이 누워 있어 가로(x)가 축이다. 끄면 세워 둔 병이라 위(y)가 축이다.")]
    [SerializeField] private bool axisAlongX;
    [Tooltip("축이 지나는 점 (병 기준)")]
    [SerializeField] private Vector3 axisOrigin;
    [Tooltip("표의 시작 위치(축 방향 좌표)")]
    [SerializeField] private float profileStart;
    [Tooltip("표의 간격(미터)")]
    [SerializeField] private float profileStep = 0.002f;
    [Tooltip("축을 따라 간격마다의 안쪽 반지름")]
    [SerializeField] private float[] profileRadii;
    [Tooltip("재료가 유리에 너무 붙지 않도록, 안쪽 반지름의 이 비율까지만 쓴다")]
    [SerializeField] private float interiorMargin = 0.97f;
    [Tooltip("바닥재 표면의 가운데(가로, 세로). 병 밖으로 나가는 재료를 이쪽으로 밀어 넣는다.")]
    [SerializeField] private Vector2 surfaceCenter;
    [Tooltip("마우스가 병 안 바닥에서 이만큼(미터)보다 멀면 놓지 않는다")]
    [SerializeField] private float pointerTolerance = 0.06f;

    /// <summary>흙의 양 한 단계 (적게, 보통, 많이). JarSetup이 단계마다 흙 모양을 만들어 둔다.</summary>
    [System.Serializable]
    public struct SoilLevel
    {
        public Mesh mesh;
        [Tooltip("이 양일 때 흙 표면의 높이 (병 기준)")]
        public float surfaceY;
        public Vector3 zoomLookPoint;
    }

    [Header("흙의 양 (JarSetup이 채운다)")]
    [SerializeField] private SoilLevel[] soilLevels;
    [Tooltip("흙 모양을 바꿔 끼울 MeshFilter (Soil 오브젝트)")]
    [SerializeField] private MeshFilter soilFilter;

    [Header("연결")]
    [Tooltip("이 병의 종류 정보 (이름 등)")]
    [SerializeField] private BottleData data;

    [Tooltip("이 병만 비추는 카메라 (RenderTexture로 출력). 평소엔 꺼 둔다.")]
    [SerializeField] private Camera viewCamera;

    [Tooltip("바닥재 원판의 Renderer. 바닥재를 바꾸면 색이 바뀐다.")]
    [SerializeField] private Renderer groundRenderer;

    [Tooltip("완성하면 켜지는 뚜껑 오브젝트. 평소엔 꺼 둔다.")]
    [SerializeField] private GameObject lid;

    [Tooltip("놓을 자리를 미리 보여 주는 반투명 재질 (Transparent). 비워 두면 미리보기가 나오지 않는다.")]
    [SerializeField] private Material ghostMaterial;

    private readonly List<PlacedMaterial> items = new List<PlacedMaterial>();
    private Color soilColor;

    // 미리보기(그림자) 상태
    private GameObject ghost;
    private MaterialData ghostOf;
    private readonly List<Material> ghostMaterials = new List<Material>();

    // 재료 모양(꼭짓점) 기억: 병 안에 들어가는지 검사할 때 쓴다
    private static readonly Dictionary<MaterialData, Vector3[]> footprints = new Dictionary<MaterialData, Vector3[]>();

    public bool IsFull => items.Count >= maxItems;
    public Vector3 ZoomLookPoint => zoomLookPoint;
    public Camera ViewCamera => viewCamera;
    public string DisplayName => data != null ? data.displayName : bottleId;
    public string BottleId => bottleId;

    /// <summary>이 병의 프리팹 (선반에 올릴 복사본을 만들 때 쓴다)</summary>
    public GameObject Prefab => data != null ? data.prefab : null;

    /// <summary>누워 있는 병인가</summary>
    public bool IsSideways => axisAlongX;
    public int ItemCount => items.Count;

    /// <summary>바닥재. null이면 흙(기본).</summary>
    public MaterialData Ground { get; private set; }

    /// <summary>완성해서 닫았으면 true. 그때부터 고칠 수 없다.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>만든 병이면 true. 만들 때 흙의 양을 정하고, 그 뒤로는 바꿀 수 없다.</summary>
    public bool IsCreated { get; private set; }

    /// <summary>흙의 양: 0 적게, 1 보통, 2 많이</summary>
    public int SoilLevelIndex { get; private set; } = 1;

    /// <summary>병을 만든다. 흙의 양(0 적게, 1 보통, 2 많이)을 정하고, 그 뒤로는 바꿀 수 없다.</summary>
    public void Create(int soilLevel)
    {
        if (IsCreated) return;
        IsCreated = true;
        ApplySoilLevel(soilLevel);
        SaveState();
    }

    /// <summary>
    /// 병을 비운다: 안의 재료와 바닥재는 모두 사라지고, 병은 만들기 전 상태로 돌아간다(같은 종류를 다시 만들 수 있다).
    /// 선반에서도 내려진다. (기획서 6.3: 선반이 다 찼을 때 병을 비워 빈 병으로 되돌린다. 안의 재료는 사라진다.)
    /// </summary>
    public void ResetToEmpty()
    {
        BottleStore.Undisplay(bottleId); // 선반 칸 번호를 먼저 지운다 (아래 Set이 이어 받지 않게)
        HideGhost();
        foreach (PlacedMaterial placed in items)
            if (placed != null) Destroy(placed.gameObject);
        items.Clear();

        Ground = null;
        ApplyGround();
        IsCompleted = false;
        IsCreated = false;
        if (lid != null) lid.SetActive(false);
        ApplySoilLevel(1);
        BottleStore.Set(bottleId, new BottleRecord());
    }

    /// <summary>흙의 양에 맞는 흙 모양과 표면 높이로 바꾼다.</summary>
    private void ApplySoilLevel(int level)
    {
        SoilLevelIndex = Mathf.Clamp(level, 0, 2);
        if (soilLevels == null || soilLevels.Length == 0) return;

        SoilLevel s = soilLevels[Mathf.Min(SoilLevelIndex, soilLevels.Length - 1)];
        if (soilFilter != null && s.mesh != null) soilFilter.sharedMesh = s.mesh;
        surfaceLocalY = s.surfaceY;
        zoomLookPoint = s.zoomLookPoint;
    }

    // 처음 켜지는 순간(Awake)에 기억해 둔 상태를 되살린다. 숨겨 뒀다가 나중에 켜지는 병도
    // 켜지자마자 완성 여부와 바닥재가 맞게 보이도록, Start가 아니라 Awake에서 한다.
    /// <summary>
    /// 선반에 올릴 복사본을 만들 때, Instantiate 직전에 병 번호(보관된 병의 번호)를 여기에 넣어 둔다.
    /// 복사본의 Awake가 이 번호의 기록으로 병 속을 되살린다. (한 번 쓰면 비워진다)
    /// </summary>
    public static string SpawnId;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(SpawnId)) { bottleId = SpawnId; SpawnId = null; }

        if (groundRenderer != null) soilColor = groundRenderer.sharedMaterial.color;

        // 씬을 옮겼다 돌아왔을 때 기억해 둔 상태를 다시 만든다.
        BottleRecord record = BottleStore.Get(bottleId);
        foreach (PlacedData placed in record.items)
            Spawn(placed.material, placed.localPosition, placed.yaw, placed.scaleFactor);

        Ground = record.ground;
        ApplyGround();
        IsCreated = record.created;
        ApplySoilLevel(record.soilLevel);
        IsCompleted = record.completed;
        if (lid != null) lid.SetActive(IsCompleted);
    }

    // ───────── 놓을 자리 찾기 (병 밖으로 나가지 않게) ─────────

    /// <summary>
    /// 화면에서 쏜 광선(월드 기준)이 닿은 바닥재 표면의 자리에, 이 재료를 놓을 수 있는지 본다.
    /// 재료의 어느 부분이든 병 밖으로 나가면 가운데 쪽으로 밀어 넣고, 어디에도 안 들어가면 TooBig.
    /// </summary>
    public PlacementResult TryFindPlacement(MaterialData material, Ray worldRay, out Vector3 localPosition)
    {
        localPosition = Vector3.zero;

        Plane surface = new Plane(transform.up, transform.TransformPoint(0f, surfaceLocalY, 0f));
        if (!surface.Raycast(worldRay, out float distance)) return PlacementResult.Outside;

        Vector3 hit = transform.InverseTransformPoint(worldRay.GetPoint(distance));
        hit.y = surfaceLocalY;
        if (Slack(hit) < -pointerTolerance) return PlacementResult.Outside; // 병 안 바닥에서 너무 멀다

        return FitInside(GetFootprint(material), 0f, 1f, hit, out localPosition)
            ? PlacementResult.Ok
            : PlacementResult.TooBig;
    }

    /// <summary>재료를 놓는다. 가득 찼거나 완성한 병이면 null. (자리는 TryFindPlacement로 구한 것을 넣는다)</summary>
    public PlacedMaterial Place(MaterialData material, Vector3 localPosition)
    {
        if (IsFull || IsCompleted || !IsCreated) return null;
        PlacedMaterial placed = Spawn(material, localPosition, 0f, 1f);
        SaveState();
        return placed;
    }

    /// <summary>놓은 재료를 뺀다.</summary>
    public void Remove(PlacedMaterial placed)
    {
        if (IsCompleted) return;
        items.Remove(placed);
        Destroy(placed.gameObject);
        SaveState();
    }

    /// <summary>놓은 재료를 돌린다. 돌린 모습이 병 밖으로 나가면 안쪽으로 밀어 보고, 그래도 안 되면 돌리지 않는다(false).</summary>
    public bool TryRotate(PlacedMaterial placed, float degrees)
    {
        if (IsCompleted) return false;
        return TryChangePose(placed, placed.Yaw + degrees, placed.ScaleFactor);
    }

    /// <summary>놓은 재료의 크기를 곱한다. 병 밖으로 나가면 안쪽으로 밀어 보고, 그래도 안 되면 바꾸지 않는다(false).</summary>
    public bool TryScale(PlacedMaterial placed, float multiplier)
    {
        if (IsCompleted) return false;
        float newScale = Mathf.Clamp(placed.ScaleFactor * multiplier, PlacedMaterial.MinScale, PlacedMaterial.MaxScale);
        if (Mathf.Approximately(newScale, placed.ScaleFactor)) return false; // 이미 한계
        return TryChangePose(placed, placed.Yaw, newScale);
    }

    private bool TryChangePose(PlacedMaterial placed, float yaw, float scaleFactor)
    {
        if (!FitInside(GetFootprint(placed.Material), yaw, scaleFactor, placed.transform.localPosition, out Vector3 fitted))
            return false;

        placed.transform.localPosition = fitted;
        placed.SetPose(yaw, scaleFactor);
        SaveState();
        return true;
    }

    // ───────── 병 안쪽 판정 ─────────

    /// <summary>
    /// 한 점이 병 안쪽에서 얼마나 여유가 있는지. 0보다 크면 안쪽, 작으면 밖(유리 속이거나 병 바깥).
    /// 축 방향 위치에서 안쪽 반지름을 표에서 읽고, 축에서 떨어진 거리와 비교한다.
    /// </summary>
    private float Slack(Vector3 p)
    {
        if (profileRadii == null || profileRadii.Length < 2) return 1f; // 표가 없으면 검사하지 않는다

        float along = axisAlongX ? p.x : p.y;
        float index = (along - profileStart) / profileStep;
        if (index < 0f || index > profileRadii.Length - 1) return -1f; // 병 길이 밖 (병 입구 위쪽 포함)

        int i0 = Mathf.FloorToInt(index);
        int i1 = Mathf.Min(i0 + 1, profileRadii.Length - 1);
        float radius = Mathf.Lerp(profileRadii[i0], profileRadii[i1], index - i0);

        Vector3 d = p - axisOrigin;
        float away = axisAlongX ? new Vector2(d.y, d.z).magnitude : new Vector2(d.x, d.z).magnitude;
        return radius * interiorMargin - away;
    }

    /// <summary>이 자리·각도·크기에서 재료의 모든 꼭짓점이 병 안쪽에 있는가</summary>
    private bool Fits(Vector3[] footprint, float yaw, float scale, Vector3 position)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        foreach (Vector3 v in footprint)
            if (Slack(position + rotation * (v * scale)) < 0f) return false;
        return true;
    }

    /// <summary>
    /// 원하는 자리에 들어가면 그대로, 아니면 바닥 가운데 쪽으로 밀어서 들어가는 가장 먼 자리를 찾는다.
    /// 가운데에서도 안 들어가면 false.
    /// </summary>
    private bool FitInside(Vector3[] footprint, float yaw, float scale, Vector3 desired, out Vector3 result)
    {
        result = desired;
        if (Fits(footprint, yaw, scale, desired)) return true;

        Vector3 center = new Vector3(surfaceCenter.x, surfaceLocalY, surfaceCenter.y);
        if (!Fits(footprint, yaw, scale, center)) return false;

        float inside = 0f, outside = 1f; // 가운데(0)는 들어가고, 원하는 자리(1)는 안 들어간다
        for (int i = 0; i < 10; i++)
        {
            float mid = (inside + outside) * 0.5f;
            if (Fits(footprint, yaw, scale, Vector3.Lerp(center, desired, mid))) inside = mid; else outside = mid;
        }
        result = Vector3.Lerp(center, desired, inside);
        return true;
    }

    /// <summary>
    /// 재료 모양의 꼭짓점들(바닥이 높이 0에 오도록 맞춘 것). 처음 한 번만 만들고 기억해 둔다.
    /// 너무 많으면 일부만 쓰고, 가장 바깥 8개 모서리는 꼭 넣는다.
    /// </summary>
    private static Vector3[] GetFootprint(MaterialData material)
    {
        if (footprints.TryGetValue(material, out Vector3[] cached)) return cached;

        var all = new List<Vector3>();
        if (material.placedPrefab != null)
        {
            Transform root = material.placedPrefab.transform;
            foreach (MeshFilter mf in material.placedPrefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices) all.Add(toRoot.MultiplyPoint3x4(v));
            }
        }
        else
        {
            PrimitiveType type = material.placeholderShape == PlaceholderShape.Capsule ? PrimitiveType.Capsule
                               : material.placeholderShape == PlaceholderShape.Cube ? PrimitiveType.Cube
                               : PrimitiveType.Sphere;
            GameObject temp = GameObject.CreatePrimitive(type);
            foreach (Vector3 v in temp.GetComponent<MeshFilter>().sharedMesh.vertices)
                all.Add(Vector3.Scale(v, material.placeholderSize));
            Destroy(temp);
        }

        if (all.Count == 0) { footprints[material] = new Vector3[0]; return footprints[material]; }

        Bounds bounds = new Bounds(all[0], Vector3.zero);
        foreach (Vector3 v in all) bounds.Encapsulate(v);
        Vector3 shift = new Vector3(0f, -bounds.min.y, 0f); // 바닥이 표면에 닿도록 (BottleContents가 모습을 올리는 것과 같다)

        var result = new List<Vector3>();
        int stride = Mathf.Max(1, all.Count / 500);
        for (int i = 0; i < all.Count; i += stride) result.Add(all[i] + shift);
        Vector3 min = bounds.min + shift, max = bounds.max + shift;
        for (int c = 0; c < 8; c++)
            result.Add(new Vector3((c & 1) == 0 ? min.x : max.x, (c & 2) == 0 ? min.y : max.y, (c & 4) == 0 ? min.z : max.z));

        footprints[material] = result.ToArray();
        return footprints[material];
    }

    // ───────── 바닥재, 완성, 미리보기, 기록 ─────────

    /// <summary>바닥재를 바꾼다. null이면 흙. (가방의 모래를 쓰고 돌려주는 일은 부르는 쪽이 한다.)</summary>
    public void SetGround(MaterialData ground)
    {
        if (IsCompleted) return;
        Ground = ground;
        ApplyGround();
        SaveState();
    }

    /// <summary>병을 완성한다. 뚜껑이 닫히고 더는 고칠 수 없다.</summary>
    public void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        HideGhost();
        if (lid != null) lid.SetActive(true);
        SaveState();
    }

    /// <summary>놓을 자리에 반투명한 재료를 미리 보여 준다. (기록에는 들어가지 않는다)</summary>
    public void ShowGhost(MaterialData material, Vector3 localPosition)
    {
        if (ghostMaterial == null || material == null) return;

        // 고른 재료가 바뀌었을 때만 새로 만든다.
        if (ghost == null || ghostOf != material)
        {
            DestroyGhost();
            ghost = BuildGhost(material);
            ghostOf = material;
        }

        ghost.transform.localPosition = localPosition;
        ghost.SetActive(true);
    }

    /// <summary>미리보기를 숨긴다.</summary>
    public void HideGhost()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    /// <summary>돌리거나 키운 뒤에 불러서 기록을 새로 한다.</summary>
    public void SaveState()
    {
        BottleRecord old = BottleStore.Get(bottleId); // 병 종류와 이름은 이어 받는다
        BottleRecord record = new BottleRecord { ground = Ground, completed = IsCompleted, created = IsCreated, soilLevel = SoilLevelIndex, typeId = old.typeId, name = old.name };
        foreach (PlacedMaterial placed in items)
        {
            record.items.Add(new PlacedData
            {
                material = placed.Material,
                localPosition = placed.transform.localPosition,
                yaw = placed.Yaw,
                scaleFactor = placed.ScaleFactor
            });
        }
        BottleStore.Set(bottleId, record);
    }

    private void ApplyGround()
    {
        if (groundRenderer == null) return;
        groundRenderer.material.color = Ground != null ? Ground.placeholderColor : soilColor;
    }

    private GameObject BuildGhost(MaterialData material)
    {
        GameObject root = new GameObject("Ghost_" + material.id);
        root.transform.SetParent(transform, false);
        root.layer = gameObject.layer;

        GameObject visual = CreateVisual(material, root.transform);

        // 만질 수 없게 Collider를 없애고, 반투명 재질로 바꾼다. (색은 재료 색을 따른다)
        foreach (Collider col in visual.GetComponentsInChildren<Collider>())
            Destroy(col);
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
        {
            Material tinted = new Material(ghostMaterial);
            Color c = material.placeholderColor;
            tinted.color = new Color(c.r, c.g, c.b, 0.5f);
            ghostMaterials.Add(tinted);
            r.sharedMaterial = tinted;
        }
        return root;
    }

    private void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
        foreach (Material m in ghostMaterials) Destroy(m);
        ghostMaterials.Clear();
        ghost = null;
        ghostOf = null;
    }

    private void OnDestroy()
    {
        DestroyGhost();
    }

    private PlacedMaterial Spawn(MaterialData material, Vector3 localPosition, float yaw, float scaleFactor)
    {
        GameObject root = new GameObject("Placed_" + material.id);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPosition;
        root.layer = gameObject.layer;

        CreateVisual(material, root.transform);

        PlacedMaterial placed = root.AddComponent<PlacedMaterial>();
        placed.Init(material, yaw, scaleFactor);
        items.Add(placed);
        return placed;
    }

    /// <summary>모습을 만든다. 모델(프리팹)이 있으면 그것을, 없으면 기본 도형을 쓴다.</summary>
    private GameObject CreateVisual(MaterialData material, Transform parent)
    {
        GameObject visual;
        if (material.placedPrefab != null)
        {
            visual = Instantiate(material.placedPrefab, parent);
        }
        else
        {
            PrimitiveType type = material.placeholderShape == PlaceholderShape.Capsule ? PrimitiveType.Capsule
                               : material.placeholderShape == PlaceholderShape.Cube ? PrimitiveType.Cube
                               : PrimitiveType.Sphere;
            visual = GameObject.CreatePrimitive(type);
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = material.placeholderSize;
            visual.GetComponent<Renderer>().material.color = material.placeholderColor;

            // 병 안에서 마우스로 고를 수 있게 Collider를 두되, 부딪히지 않게 Trigger로 한다.
            Collider col = visual.GetComponent<Collider>();
            col.isTrigger = true;
        }

        SetLayerRecursive(visual, gameObject.layer);

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            // 모델(프리팹)에는 Collider가 없어서, 병 안에서 마우스로 고를 수 있게 모델을 감싸는 상자를 붙인다.
            if (material.placedPrefab != null && visual.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider box = visual.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = visual.transform.InverseTransformPoint(bounds.center);
                box.size = bounds.size; // 만들 때는 회전과 크기가 그대로라 월드 크기와 같다
            }

            // 바닥이 바닥재 표면에 닿도록 올린다.
            float lift = parent.position.y - bounds.min.y;
            visual.transform.position += Vector3.up * lift;
        }
        return visual;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }

    // Play를 껐다 켤 때 기억해 둔 모양이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        footprints.Clear();
    }
}
