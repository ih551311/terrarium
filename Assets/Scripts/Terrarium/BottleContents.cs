using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 병 하나의 속(바닥재 표면 위에 놓인 재료들)을 관리한다.
/// 병 뿌리 오브젝트(Bottle)에 붙인다. 놓은 재료는 병의 자식이라 병과 함께 돈다.
/// 규칙: 재료는 바닥재 표면 위에만 놓이고, 물리 효과는 없고, 서로 겹쳐도 되고, 병 하나에 12개까지다.
/// </summary>
public class BottleContents : MonoBehaviour
{
    [Tooltip("이 병을 구별하는 이름 (병마다 달라야 한다). 예: bottle0")]
    [SerializeField] private string bottleId = "bottle0";

    [Tooltip("바닥재 표면의 높이 (병 기준 위쪽 방향 거리)")]
    [SerializeField] private float surfaceLocalY = 0.07f;

    [Tooltip("재료를 놓을 수 있는 바닥재의 반지름")]
    [SerializeField] private float soilRadius = 0.17f;

    [SerializeField] private int maxItems = 12;

    [Tooltip("놓을 자리를 미리 보여 주는 반투명 재질 (Transparent). 비워 두면 미리보기가 나오지 않는다.")]
    [SerializeField] private Material ghostMaterial;

    private readonly List<PlacedMaterial> items = new List<PlacedMaterial>();

    // 미리보기(그림자) 상태
    private GameObject ghost;
    private MaterialData ghostOf;
    private readonly List<Material> ghostMaterials = new List<Material>();

    public bool IsFull => items.Count >= maxItems;

    private void Start()
    {
        // 씬을 옮겼다 돌아왔을 때 기억해 둔 재료를 다시 놓는다.
        foreach (PlacedData data in BottleStore.Get(bottleId))
            Spawn(data.material, data.localPosition, data.yaw, data.scaleFactor);
    }

    /// <summary>
    /// 화면에서 쏜 광선(월드 기준)이 바닥재 표면의 놓을 수 있는 곳에 닿는지 본다.
    /// 닿으면 true와 병 기준 위치를 돌려준다. 공중이나 병 바깥이면 false.
    /// </summary>
    public bool TryGetSurfacePoint(Ray worldRay, out Vector3 localPoint)
    {
        localPoint = Vector3.zero;

        Plane surface = new Plane(transform.up, transform.TransformPoint(0f, surfaceLocalY, 0f));
        if (!surface.Raycast(worldRay, out float distance)) return false;

        localPoint = transform.InverseTransformPoint(worldRay.GetPoint(distance));
        localPoint.y = surfaceLocalY;
        return new Vector2(localPoint.x, localPoint.z).magnitude <= soilRadius;
    }

    /// <summary>재료를 놓는다. 가득 찼으면 null.</summary>
    public PlacedMaterial Place(MaterialData material, Vector3 localPosition)
    {
        if (IsFull) return null;
        PlacedMaterial placed = Spawn(material, localPosition, 0f, 1f);
        SaveState();
        return placed;
    }

    /// <summary>놓은 재료를 뺀다.</summary>
    public void Remove(PlacedMaterial placed)
    {
        items.Remove(placed);
        Destroy(placed.gameObject);
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

    /// <summary>돌리거나 키운 뒤에 불러서 기록을 새로 한다.</summary>
    public void SaveState()
    {
        List<PlacedData> list = new List<PlacedData>();
        foreach (PlacedMaterial placed in items)
        {
            list.Add(new PlacedData
            {
                material = placed.Material,
                localPosition = placed.transform.localPosition,
                yaw = placed.Yaw,
                scaleFactor = placed.ScaleFactor
            });
        }
        BottleStore.Set(bottleId, list);
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

        // 바닥이 바닥재 표면에 닿도록 올린다.
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
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
}
