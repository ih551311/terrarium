using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1인칭 걷기와 마우스 둘러보기.
/// 달리기와 점프는 기획서에 따라 넣지 않았다.
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
    [SerializeField] private float gravity = -20f;

    [Header("시선")]
    [Tooltip("마우스 움직임 한 픽셀당 회전 각도")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController controller;
    private float pitch;          // 위아래 시선 각도
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

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
        Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;

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
        move *= walkSpeed;

        // 땅에 붙어 있게 하는 중력
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
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
