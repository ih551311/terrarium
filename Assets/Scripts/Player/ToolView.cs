using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 오른쪽 아래에 나타나 휘둘러지는 도끼와 곡괭이. 모델 파일이 없어서 상자로 만든 임시 모양이고,
/// 휘두르는 움직임은 애니메이션 없이 코드로 회전시킨다. 손은 보이지 않는다.
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

    private GameObject axe, pickaxe;
    private Renderer axeHead, pickaxeHead;
    private Coroutine swinging;

    /// <summary>카메라 아래에 도구 보기를 만든다.</summary>
    public static ToolView Create(Transform camera)
    {
        GameObject go = new GameObject("ToolView");
        go.transform.SetParent(camera, false);
        go.transform.localPosition = new Vector3(0.3f, -0.3f, 0.75f);
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

        axe.SetActive(false);
        pickaxe.SetActive(false);
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
        GameObject model = tool == AcquireMethod.Axe ? axe : pickaxe;
        Renderer head = tool == AcquireMethod.Axe ? axeHead : pickaxeHead;
        head.material.color = HeadColors[Mathf.Clamp(level, 0, HeadColors.Length - 1)];

        if (swinging != null) StopCoroutine(swinging);
        swinging = StartCoroutine(SwingRoutine(model));
    }

    private IEnumerator SwingRoutine(GameObject model)
    {
        axe.SetActive(false);
        pickaxe.SetActive(false);
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
