using System.Collections;
using UnityEngine;

/// <summary>
/// 도끼나 곡괭이로 캘 수 있는 나무와 돌. 나무나 돌 오브젝트(Collider가 있는 것)에 붙인다.
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

    // 비어 있을 때: 색을 살짝 줄이고 은은한 빛을 더해 "바랜" 느낌을 낸다. (곱하기만으로는 어두워지기만 해서)
    private static readonly Color EmptyTint = new Color(0.88f, 0.88f, 0.88f, 1f);
    private static readonly Color EmptyGlow = new Color(0.17f, 0.17f, 0.16f, 1f);

    private int hits;
    private bool depleted;
    private string id;
    private Renderer[] renderers;
    private Material[][] originals; // 원래 재질 (비웠다가 되돌릴 때 쓴다)
    private Coroutine shaking;
    private Vector3 restPosition;

    public bool IsEmpty => depleted;

    public string Prompt
    {
        get
        {
            if (depleted) return "오늘은 비어 있어요";
            return tool == AcquireMethod.Axe ? "[좌클릭] 도끼로 캐기" : "[좌클릭] 곡괭이로 캐기";
        }
    }

    private void Awake()
    {
        Vector3 p = transform.position;
        id = "h_" + p.x.ToString("F1") + "_" + p.z.ToString("F1");
        restPosition = transform.position;
        renderers = GetComponentsInChildren<Renderer>();

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
            first = settings.Roll(tool, zone, level.rareChance, out bool firstRare);
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
        ToolSound.Play(transform.position, last ? 0.8f : 1f);
        Shake();

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
            if (i > 0) m = settings.Roll(tool, zone, level.rareChance, out rare);
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
        if (originals == null)
        {
            originals = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++) originals[i] = renderers[i] != null ? renderers[i].sharedMaterials : null;
        }

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
                    faded[k].SetColor("_BaseColor", EmptyTint);
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

    // ───────── 흔들림 ─────────

    private void Shake()
    {
        if (shaking != null) StopCoroutine(shaking);
        transform.position = restPosition;
        shaking = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        const float Duration = 0.22f;
        float t = 0f;
        while (t < Duration)
        {
            t += Time.deltaTime;
            float strength = 1f - t / Duration;
            Vector3 jitter = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 0.06f * strength;
            transform.position = restPosition + jitter;
            yield return null;
        }
        transform.position = restPosition;
        shaking = null;
    }
}
