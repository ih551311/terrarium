using UnityEngine;

/// <summary>
/// 1인칭으로 보이는 도구(도끼, 곡괭이, 괭이)와 그것을 쥔 오른손. Interactor가 카메라 아래에 만든다.
/// - 캘 수 있는 나무, 돌, 풀을 바라보면 그 도구를 화면 오른쪽 아래에서 들어 올리고(SetAim), 시선을 돌리면 잠시 뒤 내려놓는다.
/// - 좌클릭하면 어깨를 축으로 들었다가(Windup) 내려치고(Strike) 잠깐 멈춘 뒤 되돌아온다. 맞는 순간은 ImpactDelay초 뒤이고,
///   Harvestable이 그때 소리와 흔들림을 낸다.
/// - 들고 있는 동안 숨쉬듯 조금 오르내리고, 걸으면 흔들리고, 시선을 돌리면 살짝 늦게 따라온다.
/// 모델은 Resources/ToolRig 프리팹(ToolModelMaker가 만든다: 팔레트 로우폴리 도구 3종과 손). 없으면 상자로 만든 임시 모양을 쓴다.
/// 강화 단계가 오르면 도구 머리 색이 바뀐다(머리는 흰색이라 색을 곱하면 그 색이 된다).
/// </summary>
public class ToolView : MonoBehaviour
{
    private static readonly Color[] HeadColors =
    {
        new Color(0.66f, 0.68f, 0.72f), // 0단계: 쇠
        new Color(0.85f, 0.56f, 0.34f), // 1단계: 구리
        new Color(0.58f, 0.80f, 0.92f), // 2단계: 푸른 쇠
        new Color(0.98f, 0.82f, 0.34f)  // 3단계: 금
    };

    // 휘두르기 시간(초): 들기, 내려치기, 멈춤, 되돌아오기
    private const float Windup = 0.10f, Strike = 0.07f, Hold = 0.06f, Recover = 0.24f;

    /// <summary>좌클릭부터 도구가 맞는 순간까지(초). 소리와 흔들림을 이때 낸다.</summary>
    public const float ImpactDelay = Windup + Strike;

    // 카메라 기준 자리(미터): 어깨(돌리는 축, 화면 밖 오른쪽 아래 뒤)와 손(자루를 쥔 곳)
    private static readonly Vector3 ShoulderPos = new Vector3(0.36f, -0.56f, 0.16f);
    private static readonly Vector3 GripPos = new Vector3(0.22f, -0.15f, 0.52f);
    private static readonly Vector3 GripEuler = new Vector3(18f, -30f, -50f); // 자루가 오른쪽 위로 비스듬히 눕고, 도구 머리 옆면이 보이게 돌린다
    private const float ModelScale = 0.7f;
    // 휘두르기 각도(도): 들어 올릴 때와 내려칠 때의 어깨와 손목. 양수는 앞으로 숙여 내려친다
    private const float RaiseShoulderDeg = -18f, RaiseWristDeg = -38f, StrikeShoulderDeg = 7f, StrikeWristDeg = 50f;

    private Transform shoulder, grip, wrist;
    private GameObject axe, pickaxe, hoe;
    private Renderer axeHead, pickaxeHead, hoeHead;

    private AcquireMethod? aim;       // 지금 바라보는 대상에 맞는 도구 (없으면 null)
    private float lastAimTime = -10f;
    private AcquireMethod shown = AcquireMethod.Axe;
    private float raise;              // 0 내려놓음 ~ 1 들고 있음
    private bool hasShown;

    private float swingTime = -1f;    // 휘두르기 시작 뒤 지난 시간 (-1이면 휘두르는 중이 아님)
    private float shoulderPitch, wristPitch, fromShoulder, fromWrist;

    private Transform cameraTransform;
    private Quaternion lastCameraRotation;
    private Vector2 sway, swayVelocity;
    private CharacterController body;
    private float bobPhase;

    /// <summary>카메라 아래에 도구 보기를 만든다.</summary>
    public static ToolView Create(Transform camera)
    {
        GameObject go = new GameObject("ToolView");
        go.transform.SetParent(camera, false);
        ToolView view = go.AddComponent<ToolView>();
        view.cameraTransform = camera;
        return view;
    }

    private void Awake()
    {
        shoulder = new GameObject("Shoulder").transform;
        shoulder.SetParent(transform, false);
        shoulder.localPosition = ShoulderPos;
        grip = new GameObject("Grip").transform;
        grip.SetParent(shoulder, false);
        grip.localPosition = GripPos - ShoulderPos;
        grip.localRotation = Quaternion.Euler(GripEuler);
        wrist = new GameObject("Wrist").transform;
        wrist.SetParent(grip, false);
        wrist.localScale = Vector3.one * ModelScale;

        GameObject rig = Resources.Load<GameObject>("ToolRig");
        if (rig != null)
        {
            GameObject inst = Instantiate(rig, wrist, false);
            inst.name = "ToolRig";
            axe = inst.transform.Find("Axe").gameObject;
            pickaxe = inst.transform.Find("Pickaxe").gameObject;
            hoe = inst.transform.Find("Hoe").gameObject;
            axeHead = axe.transform.Find("Head").GetComponent<Renderer>();
            pickaxeHead = pickaxe.transform.Find("Head").GetComponent<Renderer>();
            hoeHead = hoe.transform.Find("Head").GetComponent<Renderer>();
        }
        else
        {
            BuildFallback();
        }
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        shoulder.gameObject.SetActive(false);
    }

    private void Start()
    {
        body = GetComponentInParent<CharacterController>();
        if (cameraTransform == null) cameraTransform = transform.parent;
        lastCameraRotation = cameraTransform != null ? cameraTransform.rotation : Quaternion.identity;
    }

    /// <summary>지금 바라보는 대상에 맞는 도구를 알려 준다 (Interactor가 매 프레임 부른다). 캘 것이 없으면 null.</summary>
    public void SetAim(AcquireMethod? tool)
    {
        aim = tool;
        if (tool.HasValue) lastAimTime = Time.time;
    }

    /// <summary>도구를 (들고 있지 않으면 들면서) 한 번 휘두른다.</summary>
    public void Swing(AcquireMethod tool, int level)
    {
        lastAimTime = Time.time;
        if (!hasShown || raise <= 0.01f || shown != tool) { Show(tool); raise = Mathf.Max(raise, 0.6f); }
        Renderer head = tool == AcquireMethod.Axe ? axeHead : tool == AcquireMethod.Hoe ? hoeHead : pickaxeHead;
        if (head != null) head.material.color = HeadColors[Mathf.Clamp(level, 0, HeadColors.Length - 1)];

        // 지금 자세에서 이어서 다시 들어 올린다 (연달아 눌러도 튀지 않게)
        fromShoulder = shoulderPitch;
        fromWrist = wristPitch;
        swingTime = 0f;
    }

    private void Show(AcquireMethod tool)
    {
        shown = tool;
        hasShown = true;
        if (axe != null) axe.SetActive(tool == AcquireMethod.Axe);
        if (pickaxe != null) pickaxe.SetActive(tool == AcquireMethod.Pickaxe);
        if (hoe != null) hoe.SetActive(tool == AcquireMethod.Hoe);
    }

    private static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
    private static float EaseIn(float t) { t = Mathf.Clamp01(t); return t * t; }
    private static float EaseInOut(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool swinging = swingTime >= 0f;

        // ── 들기와 내려놓기: 바라보는 도구가 있거나 휘두르는 중이면 든다. 바라보지 않아도 0.7초는 들고 있다.
        //    다른 도구로 바뀌면 먼저 내렸다가 바꿔 든다.
        bool wantUp = swinging || Time.time - lastAimTime < 0.7f;
        if (aim.HasValue && (!hasShown || aim.Value != shown) && !swinging)
        {
            if (raise <= 0.02f || !hasShown) Show(aim.Value);
            else wantUp = false;
        }
        float speed = wantUp ? 1f / 0.22f : 1f / 0.26f;
        raise = Mathf.MoveTowards(raise, wantUp ? 1f : 0f, dt * speed);
        bool visible = raise > 0.001f;
        if (shoulder.gameObject.activeSelf != visible) shoulder.gameObject.SetActive(visible);
        if (!visible) { swingTime = -1f; shoulderPitch = wristPitch = 0f; return; }

        // ── 휘두르기: 어깨와 손목 각도 (양수는 앞으로 숙여 내려친다)
        const float RaisedShoulder = RaiseShoulderDeg, RaisedWrist = RaiseWristDeg, StruckShoulder = StrikeShoulderDeg, StruckWrist = StrikeWristDeg;
        if (swinging)
        {
            swingTime += dt;
            float t = swingTime;
            if (t < Windup)
            {
                float k = EaseOut(t / Windup);
                shoulderPitch = Mathf.Lerp(fromShoulder, RaisedShoulder, k);
                wristPitch = Mathf.Lerp(fromWrist, RaisedWrist, k);
            }
            else if ((t -= Windup) < Strike)
            {
                float k = EaseIn(t / Strike);
                shoulderPitch = Mathf.Lerp(RaisedShoulder, StruckShoulder, k);
                wristPitch = Mathf.Lerp(RaisedWrist, StruckWrist, k);
            }
            else if ((t -= Strike) < Hold)
            {
                float shake = Mathf.Sin(t * 90f) * (1f - t / Hold) * 1.6f; // 맞은 순간의 떨림
                shoulderPitch = StruckShoulder + shake;
                wristPitch = StruckWrist;
            }
            else if ((t -= Hold) < Recover)
            {
                float k = EaseInOut(t / Recover);
                shoulderPitch = Mathf.Lerp(StruckShoulder, 0f, k);
                wristPitch = Mathf.Lerp(StruckWrist, 0f, k);
            }
            else
            {
                shoulderPitch = wristPitch = 0f;
                swingTime = -1f;
            }
        }

        // ── 시선을 돌리면 살짝 늦게 따라온다
        if (cameraTransform != null)
        {
            Quaternion delta = Quaternion.Inverse(lastCameraRotation) * cameraTransform.rotation;
            lastCameraRotation = cameraTransform.rotation;
            Vector3 e = delta.eulerAngles;
            float yaw = Mathf.DeltaAngle(0f, e.y), pitch = Mathf.DeltaAngle(0f, e.x);
            Vector2 target = new Vector2(Mathf.Clamp(-yaw * 0.0022f, -0.03f, 0.03f), Mathf.Clamp(pitch * 0.0018f, -0.025f, 0.025f));
            sway = Vector2.SmoothDamp(sway, target, ref swayVelocity, 0.09f, Mathf.Infinity, dt);
        }

        // ── 걸으면 흔들리고, 서 있으면 숨쉬듯 오르내린다
        float moving = 0f;
        if (body != null)
        {
            Vector3 v = body.velocity; v.y = 0f;
            moving = Mathf.Clamp01(v.magnitude / 3.5f);
            bobPhase += dt * 9f * moving;
        }
        float breathe = Mathf.Sin(Time.time * 1.7f) * 0.0035f * (1f - moving);
        Vector3 bob = new Vector3(Mathf.Cos(bobPhase) * 0.007f, -Mathf.Abs(Mathf.Sin(bobPhase)) * 0.012f, 0f) * moving + new Vector3(0f, breathe, 0f);

        // ── 들어 올린 정도: 내려놓을 때는 아래로 빠지며 앞으로 숙여진다
        float lowered = 1f - EaseOut(raise);
        shoulder.localPosition = ShoulderPos + new Vector3(sway.x, sway.y, 0f) + bob + new Vector3(0.04f, -0.22f, 0f) * lowered;
        shoulder.localRotation = Quaternion.Euler(shoulderPitch + 38f * lowered, 0f, 0f);
        wrist.localRotation = Quaternion.Euler(wristPitch, 0f, 0f);
    }

    // ───────── 프리팹이 없을 때: 상자로 만든 임시 도구 (손은 없다) ─────────

    private void BuildFallback()
    {
        Color handle = new Color(0.45f, 0.30f, 0.18f);
        axe = new GameObject("Axe"); axe.transform.SetParent(wrist, false);
        Part(axe.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.04f, 0.6f, 0.04f), handle);
        axeHead = Part(axe.transform, new Vector3(0f, 0.45f, 0.07f), new Vector3(0.03f, 0.1f, 0.12f), Color.white);
        pickaxe = new GameObject("Pickaxe"); pickaxe.transform.SetParent(wrist, false);
        Part(pickaxe.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.04f, 0.6f, 0.04f), handle);
        pickaxeHead = Part(pickaxe.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.03f, 0.03f, 0.34f), Color.white);
        hoe = new GameObject("Hoe"); hoe.transform.SetParent(wrist, false);
        Part(hoe.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.04f, 0.6f, 0.04f), handle);
        hoeHead = Part(hoe.transform, new Vector3(0f, 0.45f, 0.08f), new Vector3(0.15f, 0.09f, 0.01f), Color.white);
    }

    private static Renderer Part(Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Renderer r = go.GetComponent<Renderer>();
        r.material.color = color;
        return r;
    }
}
