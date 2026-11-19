using UnityEngine;

/// <summary>
/// 섬 집의 현관문. 바라보고 E를 누르면 경첩(문 오른쪽)을 축으로 집 안쪽으로 열리고, 다시 E를 누르면 닫힌다.
/// 열려 있는 동안에는 문짝의 충돌을 꺼서 지나갈 수 있고, 바라보는 상자(DoorTrigger)는 늘 켜 두어 열린 문도 닫을 수 있다.
/// 문 오브젝트의 위치가 경첩이다. HouseDoorBuilder(메뉴: 유리병 정원 > 집 문 만들기)가 만든다.
/// </summary>
public class HouseDoor : MonoBehaviour, IInteractable
{
    [Tooltip("문짝의 충돌. 열려 있는 동안 꺼진다")]
    [SerializeField] private Collider leafCollider;
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float openSeconds = 0.7f;

    private bool open;
    private float angle;          // 지금 각도 (0 닫힘 ~ openAngle 열림)
    private Quaternion closedRotation;

    /// <summary>지금 열려 있는가 (열리는 중 포함)</summary>
    public bool IsOpen => open;

    public string Prompt => open ? "[E] 문 닫기" : "[E] 문 열기";

    private void Awake()
    {
        closedRotation = transform.rotation;
    }

    public void Interact(Interactor who)
    {
        if (open && PlayerInDoorway(who)) return; // 문 한가운데 서 있으면 닫지 않는다
        open = !open;
        GameAudio.Play(open ? GameAudio.Sfx.DoorOpen : GameAudio.Sfx.DoorClose, 0.8f);
        if (open && leafCollider != null) leafCollider.enabled = false; // 열리기 시작하면 바로 지나갈 수 있다
    }

    private void Update()
    {
        float target = open ? openAngle : 0f;
        if (Mathf.Approximately(angle, target)) return;

        // 열릴 때는 천천히 시작해서 부드럽게 멈추고, 닫힐 때는 끝에서 살짝 "쿵" 하고 멈춘다
        float speed = openAngle / openSeconds;
        float next = Mathf.MoveTowards(angle, target, speed * Time.deltaTime);
        float k = Mathf.SmoothStep(0f, 1f, next / openAngle);
        angle = next;
        transform.rotation = closedRotation * Quaternion.Euler(0f, openAngle * k, 0f);

        if (!open && Mathf.Approximately(angle, 0f) && leafCollider != null) leafCollider.enabled = true;
    }

    private bool PlayerInDoorway(Interactor who)
    {
        Vector3 local = transform.InverseTransformPoint(who.transform.position);
        // 경첩 기준 문 폭 안쪽, 문 앞뒤 0.9m 안
        return local.x < 0.1f && local.x > -1.6f && Mathf.Abs(local.z) < 0.9f;
    }
}
