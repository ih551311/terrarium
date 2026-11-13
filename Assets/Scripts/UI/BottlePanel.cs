using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 꾸미기 패널. 작업대(BottleStation)가 Open()을 불러 열고, 닫기 버튼이나 Esc로 닫는다.
/// 패널 안의 "병 고르기"에서 둥근 병, 긴 병, 넓은 병 중 꾸밀 병을 고른다.
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
    [Tooltip("병 고르기 화면 (패널을 열면 이것이 먼저 보인다)")]
    [SerializeField] private BottleChooser chooser;
    [Tooltip("병 그림(RawImage) 오브젝트. 병을 고르기 전에는 숨긴다.")]
    [SerializeField] private GameObject bottleView;
    [Tooltip("꾸미기 화면에서 병 고르기 화면으로 돌아가는 버튼")]
    [SerializeField] private Button backButton;

    [Header("꾸미는 중 / 완성 후 모드")]
    [Tooltip("재료 칸, 바닥재, 완성 버튼 등 꾸밀 때만 보이는 것을 담은 오브젝트")]
    [SerializeField] private GameObject editTools;
    [Tooltip("완성한 병을 열었을 때 보이는 글자")]
    [SerializeField] private GameObject completedLabel;

    private bool isOpen;
    private bool choosing; // true: 병 고르기 화면, false: 꾸미기 화면
    private BottleStation station;
    private BottleContents contents;
    private Camera bottleCamera;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        chooser.Chosen += ShowBottle;
        backButton.onClick.AddListener(ShowChooser);
    }

    private void OnDisable()
    {
        if (isOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        // 가방이 위에 열려 있으면 가방이 먼저 닫히도록 양보한다.
        // Esc: 꾸미는 중이면 병 고르기로 돌아가고, 병 고르기 화면이면 패널을 닫는다.
        if (isOpen && !InventoryPanel.IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (choosing) Close();
            else ShowChooser();
        }
    }

    /// <summary>작업대의 병들을 받아서 열고, 먼저 꾸밀 병을 고르는 화면을 보여 준다.</summary>
    public void Open(BottleStation bottleStation)
    {
        if (isOpen) return;
        isOpen = true;
        station = bottleStation;

        root.SetActive(true);
        ShowChooser();
        UIFocus.Open();
    }

    /// <summary>병 고르기 화면을 보인다. (보고 있던 병은 놓고, 병 그림과 꾸미기 도구는 숨긴다)</summary>
    private void ShowChooser()
    {
        if (station == null) return;
        ReleaseBottle();
        choosing = true;

        chooser.gameObject.SetActive(true);
        chooser.Refresh(station);
        bottleView.SetActive(false);
        editTools.SetActive(false);
        completedLabel.SetActive(false);
        backButton.gameObject.SetActive(false);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        ReleaseBottle();
        station = null;

        root.SetActive(false);
        UIFocus.Close();
    }

    /// <summary>번호의 병을 작업대에 올리고 패널에 보여 준다.</summary>
    private void ShowBottle(int index)
    {
        if (station == null) return;
        ReleaseBottle();
        choosing = false;
        chooser.gameObject.SetActive(false);
        bottleView.SetActive(true);
        backButton.gameObject.SetActive(true);

        station.Select(index);
        contents = station.Get(index);
        bottleCamera = contents.ViewCamera;

        dragRotate.SetTarget(contents.GetComponent<BottleViewer>());
        decorator.SetTarget(contents, bottleCamera);
        groundSelector.SetTarget(contents);
        completeButton.SetTarget(contents);
        if (bottleCamera != null) bottleCamera.enabled = true;

        RefreshMode();
    }

    /// <summary>보고 있던 병을 놓는다. (카메라를 끄고 도구들의 대상을 비운다)</summary>
    private void ReleaseBottle()
    {
        if (bottleCamera != null) bottleCamera.enabled = false;
        bottleCamera = null;
        contents = null;
        decorator.SetTarget(null, null);
        groundSelector.SetTarget(null);
        completeButton.SetTarget(null);
    }

    /// <summary>병이 완성됐는지에 따라 꾸미기 도구를 보이거나 숨긴다.</summary>
    public void RefreshMode()
    {
        if (choosing) return; // 병 고르기 화면에서는 꾸미기 도구를 보이지 않는다
        bool done = contents != null && contents.IsCompleted;
        editTools.SetActive(!done);
        completedLabel.SetActive(done);
    }
}
