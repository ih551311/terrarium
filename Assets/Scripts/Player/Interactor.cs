using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 바라보는 물건과 E 키로 상호작용한다. (줍기, 작업대 쓰기. 물 주기와 자기는 이후 단계)
/// 바라보는 물건이 IInteractable이면 안내 글자(Hud)를 보여 주고, E를 누르면 Interact를 부른다.
/// Player 오브젝트에 붙인다. Bag과 같은 오브젝트여야 한다.
/// </summary>
[RequireComponent(typeof(Bag))]
public class Interactor : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("시선 방향을 재는 카메라 (Player의 자식 Main Camera)")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("안내 글자와 알림을 보여 주는 HUD")]
    [SerializeField] private Hud hud;

    [Header("설정")]
    [SerializeField] private float reachDistance = 2.5f;
    [SerializeField] private float messageSeconds = 2f;

    private IInteractable target;

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
            if (hud != null) hud.SetPrompt(null);
            return;
        }

        target = FindTarget();
        if (hud != null) hud.SetPrompt(target != null ? target.Prompt : null);

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && target != null && keyboard.eKey.wasPressedThisFrame)
            target.Interact(this);
    }

    /// <summary>화면 아래쪽에 알림 글자를 잠깐 보여 준다.</summary>
    public void ShowMessage(string text)
    {
        if (hud != null) hud.ShowMessage(text, messageSeconds);
    }

    private IInteractable FindTarget()
    {
        if (cameraTransform == null) return null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reachDistance, ~0, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<IInteractable>();
        return null;
    }
}
