using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 바라보는 물건과 E 키로 상호작용한다. 지금은 재료 줍기만 한다.
/// (물 주기, 작업대, 자기는 이후 단계에서 여기에 더한다.)
/// Player 오브젝트에 붙인다. Bag과 같은 오브젝트여야 한다.
/// </summary>
[RequireComponent(typeof(Bag))]
public class Interactor : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("시선 방향을 재는 카메라 (Player의 자식 Main Camera)")]
    [SerializeField] private Transform cameraTransform;

    [Header("설정")]
    [SerializeField] private float reachDistance = 2.5f;
    [SerializeField] private float messageSeconds = 2f;

    private Bag bag;
    private Pickup target;
    private string message;
    private float messageUntil;

    private void Awake()
    {
        bag = GetComponent<Bag>();
    }

    private void Update()
    {
        target = FindTarget();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && target != null && keyboard.eKey.wasPressedThisFrame)
            TryPickUp(target);
    }

    private Pickup FindTarget()
    {
        if (cameraTransform == null) return null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, ~0, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<Pickup>();
        return null;
    }

    private void TryPickUp(Pickup pickup)
    {
        if (bag.TryAdd(pickup.Material))
        {
            pickup.Collect();
            target = null;
        }
        else
        {
            message = "가방이 가득 찼어요";
            messageUntil = Time.time + messageSeconds;
        }
    }

    // 임시 화면 표시. 정식 UI는 11단계에서 만든다.
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            font = GameFont.Main,
            fontSize = 44, // 이 글꼴은 같은 값에서도 작게 그려져서 크게 잡는다
            alignment = TextAnchor.MiddleCenter
        };
        float w = 400f, h = 40f;
        float x = (Screen.width - w) / 2f;

        if (target != null && target.Material != null)
            GUI.Label(new Rect(x, Screen.height / 2f + 40f, w, h),
                "[E] " + target.Material.displayName + " 줍기", style);

        if (message != null && Time.time < messageUntil)
            GUI.Label(new Rect(x, Screen.height / 2f + 80f, w, h), message, style);
    }
}
