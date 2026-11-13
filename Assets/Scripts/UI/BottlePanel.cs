using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 꾸미기 패널. 작업대(BottleStation)가 Open()을 불러 열고, 닫기 버튼이나 Esc로 닫는다.
///
/// 흐름:
/// 1) 꾸미는 중인 병이 없으면 "병 만들기" 화면이 먼저 나온다. 흙의 양을 고르고 병 카드를 누르면 병이 만들어진다.
///    병의 종류와 흙의 양은 이때 정해지고, 그 뒤로는 바꿀 수 없다. (지금은 병을 바로 주지만, 나중에는 병을 사야 한다)
/// 2) 만든 병은 병 그림 화면에서 꾸민다. 꾸미는 중에는 병을 바꾸는 버튼이 없다. 패널을 닫았다 열면 그 병이 그대로 열린다.
/// 3) 완성하면 꾸미기 도구는 사라지고 "새 병 만들기" 버튼이 나온다. 누르면 1)로 돌아간다.
/// 병은 병 옆 카메라가 RenderTexture에 그려서 패널 안 RawImage로 보여 준다.
/// 패널은 GameRoot 안에 있어서 씬을 옮겨도 남고, 병과 카메라는 열 때마다 받는다.
/// </summary>
public class BottlePanel : MonoBehaviour
{
    [Tooltip("켜고 끄는 패널 화면 전체")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [Tooltip("병 그림 위에서 끌어 병 방향을 바꾸는 것을 받는 스크립트")]
    [SerializeField] private DragRotate dragRotate;
    [Tooltip("병 그림 위에서 재료를 놓고 빼는 것을 받는 스크립트")]
    [SerializeField] private BottleDecorator decorator;
    [SerializeField] private GroundSelector groundSelector;
    [SerializeField] private CompleteBottleButton completeButton;
    [Tooltip("병 만들기 화면 (꾸미는 중인 병이 없을 때 먼저 보인다)")]
    [SerializeField] private BottleChooser chooser;
    [Tooltip("병 그림(RawImage) 오브젝트. 병을 만들기 전에는 숨긴다.")]
    [SerializeField] private GameObject bottleView;
    [Tooltip("완성한 병을 보고 있을 때 나오는, 새 병을 만들러 가는 버튼")]
    [SerializeField] private Button newBottleButton;
    [Tooltip("완성한 병을 보고 있을 때 나오는, 선반에 전시하거나 내리는 버튼")]
    [SerializeField] private Button displayButton;
    [SerializeField] private TMPro.TMP_Text displayLabel;

    [Header("꾸미는 중 / 완성 후 모드")]
    [Tooltip("재료 칸, 바닥재, 완성 버튼 등 꾸밀 때만 보이는 것을 담은 오브젝트")]
    [SerializeField] private GameObject editTools;
    [Tooltip("완성한 병을 열었을 때 보이는 글자")]
    [SerializeField] private GameObject completedLabel;

    private bool isOpen;
    private bool choosing; // true: 병 만들기 화면, false: 병 그림 화면
    private BottleStation station;
    private BottleContents contents;
    private Camera bottleCamera;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        chooser.Chosen += OnBottleChosen;
        newBottleButton.onClick.AddListener(ShowChooser);
        displayButton.onClick.AddListener(ToggleDisplay);
    }

    /// <summary>보고 있는 완성한 병을 선반에 전시하거나, 이미 전시 중이면 내린다.</summary>
    private void ToggleDisplay()
    {
        if (contents == null || !contents.IsCompleted) return;
        string id = contents.BottleId;
        if (BottleStore.IsDisplayed(id))
        {
            BottleStore.Undisplay(id);
            decorator.ShowMessage("선반에서 내렸어요");
        }
        else if (BottleStore.TryDisplay(id))
        {
            decorator.ShowMessage("선반에 전시했어요");
        }
        else
        {
            decorator.ShowMessage("선반이 가득 찼어요");
        }
        RefreshMode();
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

    /// <summary>작업대의 병들을 받아서 연다. 꾸미는 중인 병이 있으면 바로 그 병을, 없으면 병 만들기 화면을 보여 준다.</summary>
    public void Open(BottleStation bottleStation)
    {
        if (isOpen) return;
        isOpen = true;
        station = bottleStation;

        root.SetActive(true);
        int inProgress = station.InProgressIndex;
        if (inProgress >= 0) ShowBottle(inProgress);
        else ShowChooser();
        UIFocus.Open();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        ReleaseBottle();
        if (station != null) station.ShowOnlyInProgress(); // 닫으면 작업대에는 꾸미는 중인 병만 남는다
        station = null;

        root.SetActive(false);
        UIFocus.Close();
    }

    /// <summary>병 만들기 화면을 보인다. (보고 있던 병은 놓고, 병 그림과 꾸미기 도구는 숨긴다)</summary>
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
        newBottleButton.gameObject.SetActive(false);
        displayButton.gameObject.SetActive(false);
    }

    /// <summary>병 카드를 눌렀다. 아직 안 만든 병이면 지금 고른 흙의 양으로 만들고(이때 정해진다), 그 병을 연다.</summary>
    private void OnBottleChosen(int index)
    {
        if (station == null) return;
        BottleContents bottle = station.Get(index);
        if (!bottle.IsCreated) bottle.Create(chooser.SelectedSoil);
        ShowBottle(index);
    }

    /// <summary>번호의 병을 작업대에 올리고 패널에 보여 준다.</summary>
    private void ShowBottle(int index)
    {
        if (station == null) return;
        ReleaseBottle();
        choosing = false;
        chooser.gameObject.SetActive(false);
        bottleView.SetActive(true);

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
        dragRotate.SetTarget(null);
        decorator.SetTarget(null, null);
        groundSelector.SetTarget(null);
        completeButton.SetTarget(null);
    }

    /// <summary>병이 완성됐는지에 따라 꾸미기 도구를 보이거나 숨긴다. (완성하면 새 병 만들기 버튼이 나온다)</summary>
    public void RefreshMode()
    {
        if (choosing) return; // 병 만들기 화면에서는 꾸미기 도구를 보이지 않는다
        bool done = contents != null && contents.IsCompleted;
        editTools.SetActive(!done);
        completedLabel.SetActive(done);
        newBottleButton.gameObject.SetActive(done);
        displayButton.gameObject.SetActive(done);
        if (done && displayLabel != null)
            displayLabel.text = BottleStore.IsDisplayed(contents.BottleId) ? "선반에서 내리기" : "선반에 전시하기";
    }
}
