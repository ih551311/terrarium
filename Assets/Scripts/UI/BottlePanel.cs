using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 꾸미기 패널. 집 안의 병(BottleObject)이 Open()을 불러 열고, 닫기 버튼이나 Esc로 닫는다.
/// 병은 병 옆 카메라가 RenderTexture에 그려서 패널 안 RawImage로 보여 준다.
/// 패널은 GameRoot 안에 있어서 씬을 옮겨도 남고, 병과 카메라는 열 때마다 받는다.
/// 완성한 병을 열면 꾸미기 도구는 숨기고 "완성한 병" 글자만 보여 준다(보기만 가능).
/// </summary>
public class BottlePanel : MonoBehaviour
{
    [Tooltip("켜고 끄는 패널 화면 전체")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [Tooltip("병 그림 위에서 끌어 돌리는 것을 받는 스크립트")]
    [SerializeField] private DragRotate dragRotate;
    [Tooltip("병 그림 위에서 재료를 놓고 빼는 것을 받는 스크립트")]
    [SerializeField] private BottleDecorator decorator;
    [SerializeField] private GroundSelector groundSelector;
    [SerializeField] private CompleteBottleButton completeButton;

    [Header("꾸미는 중 / 완성 후 모드")]
    [Tooltip("재료 칸, 바닥재, 완성 버튼 등 꾸밀 때만 보이는 것을 담은 오브젝트")]
    [SerializeField] private GameObject editTools;
    [Tooltip("완성한 병을 열었을 때 보이는 글자")]
    [SerializeField] private GameObject completedLabel;

    private bool isOpen;
    private Camera bottleCamera;
    private BottleContents contents;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (isOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        // 가방이 위에 열려 있으면 가방이 먼저 닫히도록 양보한다.
        if (isOpen && !InventoryPanel.IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            Close();
    }

    /// <summary>어떤 병을, 어떤 카메라로 보여 줄지 받아서 연다.</summary>
    public void Open(BottleViewer bottle, Camera camera)
    {
        if (isOpen) return;
        isOpen = true;

        bottleCamera = camera;
        contents = bottle.GetComponent<BottleContents>();
        dragRotate.SetTarget(bottle);
        decorator.SetTarget(contents, camera);
        groundSelector.SetTarget(contents);
        completeButton.SetTarget(contents);
        if (bottleCamera != null) bottleCamera.enabled = true;

        root.SetActive(true);
        RefreshMode();
        UIFocus.Open();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (bottleCamera != null) bottleCamera.enabled = false;
        bottleCamera = null;
        contents = null;
        decorator.SetTarget(null, null);
        groundSelector.SetTarget(null);
        completeButton.SetTarget(null);

        root.SetActive(false);
        UIFocus.Close();
    }

    /// <summary>병이 완성됐는지에 따라 꾸미기 도구를 보이거나 숨긴다.</summary>
    public void RefreshMode()
    {
        bool done = contents != null && contents.IsCompleted;
        editTools.SetActive(!done);
        completedLabel.SetActive(done);
    }
}
