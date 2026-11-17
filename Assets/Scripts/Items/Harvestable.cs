using System.Collections;
using UnityEngine;

/// <summary>
/// 도끼, 곡괭이, 괭이로 캘 수 있는 나무, 돌, 풀과 꽃. 나무, 돌, 풀 오브젝트(Collider가 있는 것)에 붙인다.
/// 보고 좌클릭하면 도구를 휘두르고, 정해진 횟수만큼 치면 재료가 가방으로 들어온다.
/// 나무와 돌은 사라지지 않는다. 캐면 "오늘은 비어 있음"으로 색이 바래고, 다음 날 아침에 돌아온다.
/// 비어 있는 상태는 집에 들어갔다 나와도 유지된다 (GameState가 기억한다).
/// 타격 수, 양, 희귀 확률은 ToolSettings 에셋에서 정한다.
/// </summary>
public class Harvestable : MonoBehaviour, IInteractable
{
    [Tooltip("이것을 캘 때 자동으로 쓰는 도구 (나무: Axe, 돌: Pickaxe)")]
    [SerializeField] private AcquireMethod tool = AcquireMethod.Axe;

    [Tooltip("이 자리의 구역. 구역에 맞는 재료가 먼저 나온다. 상관없으면 Anywhere")]
    [SerializeField] private MaterialZone zone = MaterialZone.Anywhere;

    [Tooltip("정해 두면 이것을 캘 때 늘 이 재료가 나온다. 예: 눈 섬의 눈사람에서 미니 눈사람")]
    [SerializeField] private MaterialData fixedMaterial;

    [Tooltip("괭이로 캐는 풀과 꽃만: 풀에서는 풀 재료, 꽃에서는 꽃 재료가 나온다")]
    [SerializeField] private PlantGroup plantGroup = PlantGroup.None;

    [Tooltip("비어 있을 때 입히는 색. 흰 눈이 덮인 나무처럼 원래 색이 매우 밝은 것은 더 어둡게 해서 비어 있는 것이 눈에 띄게 한다")]
    [SerializeField] private Color emptyTint = new Color(0.9f, 0.9f, 0.9f, 1f);

    // 비어 있을 때: 색을 살짝 줄이고 은은한 빛을 더해 "바랜" 느낌을 낸다. (곱하기만으로는 어두워지기만 해서)
    private static readonly Color EmptyGlow = new Color(0.05f, 0.05f, 0.045f, 1f);

    // 잎 렌더러의 이름: 줄기 이름 뒤에 소문자 l이 붙고 _LOD 번호가 온다 (예: Tree10Bl_LOD0)
    private static readonly System.Text.RegularExpressions.Regex LeafName =
        new System.Text.RegularExpressions.Regex(@"[A-Z]l_LOD\d+$");

    private int hits;
    private bool depleted;
    private string id;
    private Renderer[] renderers;
    private Material[][] originals; // 원래 재질 (비웠다가 되돌릴 때 쓴다)
    private Coroutine shaking;
    private Vector3 restPosition;

    public bool IsEmpty => depleted;

    /// <summary>이것을 캘 때 쓰는 도구 (ToolView가 바라볼 때 그 도구를 든다)</summary>
    public AcquireMethod Tool => tool;

    public string Prompt
    {
        get
        {
            if (depleted) return "오늘은 비어 있어요";
            if (fixedMaterial != null) return "[좌클릭] " + (tool == AcquireMethod.Axe ? "도끼" : tool == AcquireMethod.Pickaxe ? "곡괭이" : "괭이") + "로 " + fixedMaterial.displayName + " 캐기";
            if (tool == AcquireMethod.Hoe) return plantGroup == PlantGroup.Flower ? "[좌클릭] 괭이로 꽃 캐기" : "[좌클릭] 괭이로 풀 캐기";
            return tool == AcquireMethod.Axe ? "[좌클릭] 도끼로 캐기" : "[좌클릭] 곡괭이로 캐기";
        }
    }

    /// <summary>캐서 나올 재료 하나를 고른다. (괭이는 풀/꽃 갈래로, 도끼와 곡괭이는 구역과 희귀 확률로)</summary>
    private MaterialData Roll(ToolSettings settings, ToolSettings.Level level, out bool rare)
    {
        if (fixedMaterial != null) { rare = false; return fixedMaterial; }
        if (tool == AcquireMethod.Hoe) return settings.RollHoe(plantGroup, zone, level.rareChance, out rare);
        return settings.Roll(tool, zone, level.rareChance, out rare);
    }

    private void Awake()
    {
        Vector3 p = transform.position;
        id = SceneKey.Prefix() + "h_" + p.x.ToString("F1") + "_" + p.z.ToString("F1");
        restPosition = transform.position;
        renderers = GetComponentsInChildren<Renderer>();
        originals = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++) originals[i] = renderers[i].sharedMaterials;

        if (GameState.IsTaken(id)) SetDepleted(true);
    }

    private void OnEnable()
    {
        GameState.DayStarted += Restore;
    }

    private void OnDisable()
    {
        GameState.DayStarted -= Restore;
    }

    /// <summary>E 키는 따로 하는 일이 없다. 캐는 것은 좌클릭이라고 알려 준다.</summary>
    public void Interact(Interactor who)
    {
        who.ShowMessage(depleted ? "오늘은 비어 있어요. 내일 아침에 다시 돌아와요" : "좌클릭으로 캘 수 있어요");
    }

    /// <summary>도구로 한 번 친다. (Interactor가 좌클릭에 부른다)</summary>
    public void Hit(Interactor who)
    {
        if (depleted)
        {
            who.ShowMessage("오늘은 비어 있어요. 내일 아침에 다시 돌아와요");
            return;
        }

        ToolSettings settings = ToolSettings.Instance;
        if (settings == null) return;
        ToolSettings.Level level = settings.Get(tool, GameState.LevelOf(tool));

        bool last = hits + 1 >= level.hits;
        MaterialData first = null;
        if (last)
        {
            // 마지막 타격 전에 가방에 자리가 있는지 본다. 가득 차면 캐지지 않고 대상도 비워지지 않는다.
            first = Roll(settings, level, out bool firstRare);
            if (first == null) return;
            if (!who.Bag.CanAdd(first))
            {
                who.ShowMessage("가방이 가득 찼어요");
                return;
            }
            Harvest(who, settings, level, first, firstRare);
        }

        hits++;
        who.ToolView.Swing(tool, GameState.LevelOf(tool));
        StartCoroutine(Impact(last ? 0.8f : 1f)); // 도구가 내려와 맞는 순간에 소리와 흔들림

        if (last) hits = 0;
    }

    private void Harvest(Interactor who, ToolSettings settings, ToolSettings.Level level, MaterialData first, bool firstRare)
    {
        // 얻은 재료를 센다 (같은 재료는 "나뭇가지 +2"처럼 합친다)
        var names = new System.Collections.Generic.List<string>();
        var counts = new System.Collections.Generic.List<int>();
        bool anyRare = false;

        for (int i = 0; i < level.amount; i++)
        {
            MaterialData m = first;
            bool rare = firstRare;
            if (i > 0) m = Roll(settings, level, out rare);
            if (m == null || !who.Bag.TryAdd(m)) continue;

            anyRare |= rare;
            int index = names.IndexOf(m.displayName);
            if (index < 0) { names.Add(m.displayName); counts.Add(1); }
            else counts[index]++;
        }

        string text = string.Empty;
        for (int i = 0; i < names.Count; i++)
            text += (text.Length > 0 ? ", " : string.Empty) + names[i] + " +" + counts[i];
        if (anyRare) text += "  (희귀!)";
        who.ShowMessage(text);

        GameState.MarkTaken(id);
        SetDepleted(true);
    }

    // ───────── 비어 있음과 되돌아옴 ─────────

    private void Restore()
    {
        hits = 0;
        SetDepleted(false);
    }

    /// <summary>
    /// 비어 있음을 색으로 보여 준다. 나무와 돌이 같이 쓰는 재질은 건드리지 않고,
    /// 비운 것에만 재질 복사본을 만들어 바랜 색을 입힌다. 돌아오면 원래 재질로 되돌리고 복사본은 지운다.
    /// </summary>
    private void SetDepleted(bool value)
    {
        depleted = value;

        // 나무는 잎을 숨기고 줄기만 남긴다. (잎 오브젝트 이름은 줄기 이름 뒤에 l이 붙는다. 예: Tree01A_LOD0 / Tree01Al_LOD0)
        if (tool == AcquireMethod.Axe && SetLeavesVisible(!value)) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || originals[i] == null) continue;

            if (value)
            {
                var faded = new Material[originals[i].Length];
                for (int k = 0; k < faded.Length; k++)
                {
                    faded[k] = new Material(originals[i][k]);
                    faded[k].SetColor("_BaseColor", emptyTint);
                    faded[k].EnableKeyword("_EMISSION");
                    faded[k].SetColor("_EmissionColor", EmptyGlow);
                }
                r.sharedMaterials = faded;
            }
            else
            {
                foreach (Material m in r.sharedMaterials)
                    if (m != null && System.Array.IndexOf(originals[i], m) < 0) Destroy(m); // 복사본만 지운다
                r.sharedMaterials = originals[i];
            }
        }
    }

    /// <summary>나무의 잎을 보이거나 숨긴다. 잎을 찾지 못하면 false를 돌려주고, 그때는 색으로 비어 있음을 보여 준다.</summary>
    private bool SetLeavesVisible(bool visible)
    {
        bool found = false;
        Material hidden = HiddenLeaf;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            // 잎이 따로 떨어진 오브젝트인 나무(예: Tree10Bl_LOD0)는 그 오브젝트를 끈다
            if (LeafName.IsMatch(r.name))
            {
                r.enabled = visible;
                found = true;
                continue;
            }

            // 줄기와 잎이 한 덩어리 메시인 나무는, 잎 재질 칸만 투명 재질로 바꾼다
            if (hidden == null) continue;
            Material[] original = originals[i];
            bool hasLeaf = false;
            var swapped = (Material[])original.Clone();
            for (int k = 0; k < swapped.Length; k++)
            {
                if (!IsLeafMaterial(original[k])) continue;
                swapped[k] = hidden;
                hasLeaf = true;
            }
            if (!hasLeaf) continue;

            r.sharedMaterials = visible ? original : swapped;
            found = true;
        }
        return found;
    }

    /// <summary>잎 재질인가. (재질 이름이 Leaves나 Grass로 시작. 야자수는 열매(Coconut)도 숨긴다. 소나무는 Coconut이 줄기라 숨기지 않는다)</summary>
    private bool IsLeafMaterial(Material m)
    {
        if (m == null) return false;
        if (m.name.StartsWith("Leaves") || m.name.StartsWith("Grass")) return true;
        return m.name == "Coconut" && name.StartsWith("Palm");
    }

    private static Material hiddenLeaf;

    /// <summary>잎 칸에 끼우는 투명 재질 (Assets/Resources/HiddenLeaf.mat)</summary>
    private static Material HiddenLeaf
    {
        get
        {
            if (hiddenLeaf == null) hiddenLeaf = Resources.Load<Material>("HiddenLeaf");
            return hiddenLeaf;
        }
    }

    // ───────── 흔들림 ─────────

    private IEnumerator Impact(float volume)
    {
        yield return new WaitForSeconds(ToolView.ImpactDelay);
        ToolSound.Play(transform.position, volume);
        Shake();
    }

    private void Shake()
    {
        if (shaking != null) StopCoroutine(shaking);
        transform.position = restPosition;
        shaking = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        // 한 번에 한 방향으로 짧게 떨리고 빠르게 잦아든다. 크기는 물체 크기에 맞춰 작게 (작은 풀은 거의 안 움직이고, 큰 나무도 몇 cm만)
        const float Duration = 0.16f;
        float amplitude = Mathf.Clamp(ObjectSize() * 0.008f, 0.004f, 0.025f);
        float angle = Random.value * Mathf.PI * 2f;
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        float t = 0f;
        while (t < Duration)
        {
            t += Time.deltaTime;
            float strength = 1f - t / Duration;
            transform.position = restPosition + dir * Mathf.Sin(t * 70f) * amplitude * strength * strength;
            yield return null;
        }
        transform.position = restPosition;
        shaking = null;
    }

    /// <summary>물체의 가장 긴 변(미터)</summary>
    private float ObjectSize()
    {
        if (renderers == null || renderers.Length == 0) return 1f;
        Bounds b = default;
        bool first = true;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return first ? 1f : Mathf.Max(b.size.x, b.size.y, b.size.z);
    }
}