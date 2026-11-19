using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1인칭 걷기, 왼쪽 Shift 달리기, 스페이스 점프, 마우스 둘러보기.
/// 점프와 달리기는 기획서에는 없지만 섬이 넓어서 사용자 요청으로 넣었다.
/// Player 오브젝트(CharacterController 포함)에 붙인다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("플레이어의 자식인 카메라. 위아래 시선 회전에 쓴다.")]
    [SerializeField] private Transform cameraTransform;

    [Header("이동")]
    [SerializeField] private float walkSpeed = 3.5f;
    [Tooltip("왼쪽 Shift를 누른 채 걸을 때의 속도")]
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float gravity = -20f;
    [Tooltip("스페이스를 눌렀을 때 뛰어오르는 높이(미터)")]
    [SerializeField] private float jumpHeight = 1.1f;

    [Header("시선")]
    [Tooltip("마우스 움직임 한 픽셀당 회전 각도")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController controller;
    private float pitch;          // 위아래 시선 각도
    private float verticalVelocity;

    // ── 걷는 느낌(카메라): 걸음마다 위아래로 출렁이고, 옆으로 걸으면 살짝 기울고, 달리면 시야가 넓어지고, 착지하면 푹 내려앉는다
    private Camera cam;
    private Vector3 camBasePos;
    private float baseFov, bobPhase, bobAmount, roll, landDip, landDipVelocity, fovBoost, lastFall;
    private bool wasGrounded = true;

    /// <summary>카메라를 순간적으로 아래로 끄덕인다 (줍기, 캐기 같은 행동의 손맛). amount는 미터.</summary>
    public static void Nod(float amount)
    {
        if (current != null) current.landDipVelocity -= amount * 13f;
    }
    private static FirstPersonController current;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        current = this;
        if (cameraTransform != null)
        {
            camBasePos = cameraTransform.localPosition;
            cam = cameraTransform.GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
        }
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        float dt = Time.deltaTime;
        Keyboard keyboard = Keyboard.current;
        bool active = !UIFocus.IsOpen && Cursor.lockState == CursorLockMode.Locked;

        Vector3 v = controller.velocity; v.y = 0f;
        float speed = active ? v.magnitude : 0f;
        bool grounded = controller.isGrounded;
        bool running = keyboard != null && keyboard.leftShiftKey.isPressed && speed > 4f;

        // 걸음: 걸을수록 커지고, 서면 천천히 가라앉는다
        float moving = grounded ? Mathf.Clamp01(speed / walkSpeed) : 0f;
        bobAmount = Mathf.MoveTowards(bobAmount, moving, dt * 6f);
        bobPhase += speed * dt * 2.3f * (grounded ? 1f : 0f);
        float strength = Mathf.Lerp(0.55f, 1.25f, Mathf.InverseLerp(walkSpeed, runSpeed, speed));
        float bobY = -Mathf.Abs(Mathf.Sin(bobPhase)) * 0.035f * bobAmount * strength;
        float bobX = Mathf.Cos(bobPhase) * 0.012f * bobAmount * strength;
        float breathe = Mathf.Sin(Time.time * 1.5f) * 0.0025f * (1f - bobAmount); // 서 있을 때 숨쉬듯

        // 착지: 떨어진 속도만큼 내려앉았다가 돌아온다. 점프하는 순간엔 살짝 낮아졌다 솟는다
        if (!grounded) lastFall = Mathf.Min(verticalVelocity, lastFall);
        if (grounded && !wasGrounded) landDipVelocity += Mathf.Clamp(lastFall, -12f, 0f) * 0.08f;
        if (grounded) lastFall = 0f;
        wasGrounded = grounded;
        // 용수철처럼 0으로 돌아온다
        landDipVelocity += (-landDip * 180f - landDipVelocity * 16f) * dt;
        landDip += landDipVelocity * dt;
        landDip = Mathf.Clamp(landDip, -0.18f, 0.06f);

        // 옆으로 걸으면 몸이 기운다
        float strafe = active && keyboard != null ? (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f) : 0f;
        roll = Mathf.Lerp(roll, -strafe * 1.6f * moving + Mathf.Sin(bobPhase) * 0.35f * bobAmount, dt * 8f);

        cameraTransform.localPosition = camBasePos + new Vector3(bobX, bobY + breathe + landDip, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, roll);

        // 달리면 시야가 넓어져 속도감이 난다
        if (cam != null)
        {
            fovBoost = Mathf.Lerp(fovBoost, running ? 7f : 0f, dt * 5f);
            cam.fieldOfView = baseFov + fovBoost;
        }
    }

    private void OnEnable()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // 가방이나 병 패널이 열려 있으면 멈춘다. (커서는 UIFocus가 관리한다)
        if (UIFocus.IsOpen) return;

        // Esc로 커서를 풀고, 화면을 클릭하면 다시 잠근다. (메뉴는 11단계에서 만든다)
        if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
            return;
        }

        Look(mouse);
        Move(keyboard);
    }

    private void Look(Mouse mouse)
    {
        // 메인 화면 설정의 마우스 감도(배율, 1이 기본)를 곱한다
        Vector2 delta = mouse.delta.ReadValue() * lookSensitivity * Mathf.Clamp(PlayerPrefs.GetFloat(MainMenu.LookKey, 1f), 0.2f, 3f);

        // 좌우는 몸 전체를, 위아래는 카메라만 돌린다.
        transform.Rotate(Vector3.up, delta.x);

        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move(Keyboard keyboard)
    {
        float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);

        Vector3 move = (transform.right * x + transform.forward * z);
        if (move.sqrMagnitude > 1f) move.Normalize(); // 대각선이 더 빠르지 않게
        move *= keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;

        // 땅에 붙어 있게 하는 중력. 땅에 서 있을 때 스페이스를 누르면 점프한다.
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
