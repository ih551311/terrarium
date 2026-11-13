using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 바라보는 물건과 E 키로 상호작용한다. (줍기, 작업대 쓰기. 물 주기와 자기는 이후 단계)
/// 바라보는 물건이 IInteractable이면 안내 글자를 보여 주고, E를 누르면 Interact를 부른다.
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

    private IInteractable target;
    private string message;
    private float messageUntil;

    public Bag Bag { get; private set; }
    public Transform CameraTransform => cameraTransform;

    private void Awake()
    {
        Bag = GetComponent<Bag>();
    }

    private void Update()
    {
        // 패널이 열려 있으면 바라보기도 E 키도 받지 않는다.
        if (UIFocus.IsOpen)
        {
            target = null;
            return;
        }

        target = FindTarget();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && target != null && keyboard.eKey.wasPressedThisFrame)
            target.Interact(this);
    }

    /// <summary>화면 가운데 글자로 안내 메시지를 잠깐 보여 준다.</summary>
    public void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.time + messageSeconds;
    }

    private IInteractable FindTarget()
    {
        if (cameraTransform == null) return null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, ~0, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<IInteractable>();
        return null;
    }

    // 임시 화면 표시. 정식 UI는 11단계에서 만든다.
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            font = GameFont.Main,
            fontSize = 36,
            alignment = TextAnchor.MiddleCenter
        };
        float w = 600f, h = 60f;
        float x = (Screen.width - w) / 2f;

        if (target != null && !string.IsNullOrEmpty(target.Prompt))
            GUI.Label(new Rect(x, Screen.height / 2f + 40f, w, h), target.Prompt, style);

        if (message != null && Time.time < messageUntil)
            GUI.Label(new Rect(x, Screen.height / 2f + 100f, w, h), message, style);
    }
}
