using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 오른쪽 아래에 나타나 휘둘러지는 도끼와 곡괭이. 모델 파일이 없어서 상자로 만든 임시 모양이고,
/// 휘두르는 움직임은 애니메이션 없이 코드로 회전시킨다. 자루 아래쪽을 쥔 손과 소매 낀 팔뚝이 도구와 함께 움직인다(AddHand).
/// 강화 단계가 오르면 머리 색이 달라진다. Interactor가 카메라 아래에 만들어 둔다.
/// </summary>
public class ToolView : MonoBehaviour
{
    private static readonly Color[] HeadColors =
    {
        new Color(0.62f, 0.64f, 0.68f), // 0단계: 쇠
        new Color(0.78f, 0.52f, 0.30f), // 1단계: 구리
        new Color(0.55f, 0.78f, 0.88f), // 2단계: 푸른 쇠
        new Color(0.95f, 0.80f, 0.32f)  // 3단계: 금
    };

    private GameObject axe, pickaxe, hoe;
    private Renderer axeHead, pickaxeHead, hoeHead;
    private Coroutine swinging;

    /// <summary>카메라 아래에 도구 보기를 만든다.</summary>
    public static ToolView Create(Transform camera)
    {
        GameObject go = new GameObject("ToolView");
        go.transform.SetParent(camera, false);
        go.transform.localPosition = new Vector3(0.3f, -0.24f, 0.75f); // 쥔 손이 화면 아래에 묻히지 않을 만큼
        go.transform.localScale = Vector3.one * 0.7f;
        return go.AddComponent<ToolView>();
    }

    private void Awake()
    {
        Color handle = new Color(0.45f, 0.30f, 0.18f);

        axe = new GameObject("Axe");
        axe.transform.SetParent(transform, false);
        Part(axe.transform, "Handle", new Vector3(0f, 0.2f, 0f), new Vector3(0.05f, 0.55f, 0.05f), handle);
        axeHead = Part(axe.transform, "Head", new Vector3(0.09f, 0.42f, 0f), new Vector3(0.2f, 0.15f, 0.04f), HeadColors[0]);

        pickaxe = new GameObject("Pickaxe");
        pickaxe.transform.SetParent(transform, false);
        Part(pickaxe.transform, "Handle", new Vector3(0f, 0.2f, 0f), new Vector3(0.05f, 0.55f, 0.05f), handle);
        pickaxeHead = Part(pickaxe.transform, "Head", new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.05f, 0.05f), HeadColors[0]);

        // 괭이: 자루 끝에 납작한 날이 앞쪽으로 꺾여 있다
        hoe = new GameObject("Hoe");
        hoe.transform.SetParent(transform, false);
        Part(hoe.transform, "Handle", new Vector3(0f, 0.2f, 0f), new Vector3(0.05f, 0.55f, 0.05f), handle);
        hoeHead = Part(hoe.transform, "Head", new Vector3(0f, 0.47f, 0.07f), new Vector3(0.17f, 0.04f, 0.14f), HeadColors[0]);

        // 세 도구 모두 자루 아래쪽을 손으로 쥐고 있다
        AddHand(axe.transform);
        AddHand(pickaxe.transform);
        AddHand(hoe.transform);

        axe.SetActive(false);
        pickaxe.SetActive(false);
        hoe.SetActive(false);
    }

    private static readonly Color Skin = new Color(0.97f, 0.80f, 0.68f);
    private static readonly Color SkinShade = new Color(0.90f, 0.70f, 0.58f);
    private static readonly Color Sleeve = new Color(0.56f, 0.70f, 0.60f);   // 연두빛 스웨터
    private static readonly Color Cuff = new Color(0.95f, 0.92f, 0.84f);     // 소맷부리

    /// <summary>
    /// 도구 자루를 쥔 오른손. 주먹(손바닥), 자루를 감싼 손가락 마디 네 개, 엄지, 그리고 화면 오른쪽 아래로 빠지는 팔뚝과 소매.
    /// 도구의 자식이라 휘두르면 같이 움직인다. 카메라는 +z를 보고 있어서 손가락은 카메라 쪽(-z)에 둔다.
    /// </summary>
    private static void AddHand(Transform tool)
    {
        GameObject hand = new GameObject("Hand");
        hand.transform.SetParent(tool, false);
        hand.transform.localScale = Vector3.one * 1.2f;
        Transform h = hand.transform;
        Shape(h, PrimitiveType.Sphere, "Palm", new Vector3(0.012f, 0.03f, 0.008f), new Vector3(0.105f, 0.115f, 0.095f), Vector3.zero, Skin);
        for (int i = 0; i < 4; i++) // 자루를 감싼 손가락 마디 (위에서 아래로 조금씩 작게)
            Shape(h, PrimitiveType.Sphere, "Knuckle" + i, new Vector3(-0.012f, 0.072f - i * 0.026f, -0.04f), new Vector3(0.052f, 0.032f, 0.04f) * (1f - i * 0.06f), Vector3.zero, i % 2 == 0 ? Skin : SkinShade);
        Shape(h, PrimitiveType.Capsule, "Thumb", new Vector3(-0.035f, 0.075f, -0.012f), new Vector3(0.03f, 0.035f, 0.03f), new Vector3(0f, 0f, 50f), Skin);
        // 손목, 소맷부리, 팔뚝 (오른쪽 아래 뒤로 빠진다)
        Shape(h, PrimitiveType.Cylinder, "Wrist", new Vector3(0.03f, -0.045f, 0.012f), new Vector3(0.065f, 0.04f, 0.065f), new Vector3(0f, 0f, 22f), Skin);
        Shape(h, PrimitiveType.Cylinder, "Cuff", new Vector3(0.05f, -0.09f, 0.018f), new Vector3(0.095f, 0.025f, 0.095f), new Vector3(0f, 0f, 22f), Cuff);
        Shape(h, PrimitiveType.Cylinder, "Sleeve", new Vector3(0.11f, -0.24f, 0.03f), new Vector3(0.11f, 0.16f, 0.11f), new Vector3(0f, 0f, 22f), Sleeve);
    }

    private static void Shape(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.Euler(euler);
        Renderer r = go.GetComponent<Renderer>();
        r.material.color = color;
        r.material.SetFloat("_Smoothness", 0.1f);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static Renderer Part(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Renderer r = go.GetComponent<Renderer>();
        r.material.color = color;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    /// <summary>도구를 보이고 한 번 휘두른다. 진행 중이던 휘두르기는 새로 시작한다.</summary>
    public void Swing(AcquireMethod tool, int level)
    {
        GameObject model = tool == AcquireMethod.Axe ? axe : tool == AcquireMethod.Hoe ? hoe : pickaxe;
        Renderer head = tool == AcquireMethod.Axe ? axeHead : tool == AcquireMethod.Hoe ? hoeHead : pickaxeHead;
        head.material.color = HeadColors[Mathf.Clamp(level, 0, HeadColors.Length - 1)];

        if (swinging != null) StopCoroutine(swinging);
        swinging = StartCoroutine(SwingRoutine(model));
    }

    private IEnumerator SwingRoutine(GameObject model)
    {
        axe.SetActive(false);
        pickaxe.SetActive(false);
        hoe.SetActive(false);
        model.SetActive(true);

        // 위로 들었다가(0.12초) 내려친다(0.1초). 잠깐 머물다 사라진다.
        const float Up = 0.12f, Down = 0.1f, Hold = 0.35f;
        // X축 회전: 음수는 머리가 나(카메라) 쪽으로 젖혀지고, 양수는 앞쪽으로 숙여진다.
        Quaternion rest = Quaternion.Euler(10f, 0f, -12f);
        Quaternion raised = Quaternion.Euler(-30f, 0f, -12f);
        Quaternion struck = Quaternion.Euler(70f, 0f, -12f);

        float t = 0f;
        while (t < Up)
        {
            t += Time.deltaTime;
            model.transform.localRotation = Quaternion.Slerp(rest, raised, t / Up);
            yield return null;
        }
        t = 0f;
        while (t < Down)
        {
            t += Time.deltaTime;
            model.transform.localRotation = Quaternion.Slerp(raised, struck, t / Down);
            yield return null;
        }
        yield return new WaitForSeconds(Hold);
        model.SetActive(false);
        swinging = null;
    }
}
