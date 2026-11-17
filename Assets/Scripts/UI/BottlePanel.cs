using TMPro;
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
/// 3) 완성하면 그 병은 자동으로 "병 인벤토리"에 들어가고, 병 만들기 화면으로 돌아간다. 같은 종류의 병도 또 만들 수 있다.
/// 4) 병 만들기 화면의 "내 병" 버튼으로 병 인벤토리를 열어, 선반에 전시할 병을 고른다.
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
    [Tooltip("내 병(병 인벤토리) 화면")]
    [SerializeField] private BottleInventoryView inventoryView;
    [Tooltip("병 그림(RawImage) 오브젝트. 병을 만들기 전에는 숨긴다.")]
    [SerializeField] private GameObject bottleView;
    [Tooltip("병을 완성했을 때 이름을 짓는 상자")]
    [SerializeField] private BottleNameDialog nameDialog;

    [Header("만들다가 취소 (병 버리기)")]
    [Tooltip("가방: 취소할 때 병 속 재료와 바닥재를 돌려받는다")]
    [SerializeField] private Bag bag;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text cancelLabel;

    [Header("꾸미는 중")]
    [Tooltip("재료 칸, 바닥재, 완성 버튼 등 꾸밀 때만 보이는 것을 담은 오브젝트")]
    [SerializeField] private GameObject editTools;

    private bool isOpen;
    private BottleStation station;
    private BottleContents contents;
    private Camera bottleCamera;
    private BottleContents namingBottle; // 완성했고 이름을 짓는 중인 병

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        chooser.Chosen += OnBottleChosen;
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        chooser.OpenInventory += ShowInventory;
        inventoryView.Back += () => ShowChooser();
    }

    private void OnDisable()
    {
        if (isOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        // 가방이 위에 열려 있으면 가방이 먼저 닫히도록 양보한다.
        if (isOpen && !InventoryPanel.IsOpen && !BottleNameDialog.IsOpen && !BottleDetailPanel.IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
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

        // 이름을 짓는 중에 닫으면 지금까지 쓴 이름(없으면 기본 이름)으로 마무리해서, 완성한 병이 작업대에 남지 않게 한다
        if (namingBottle != null && nameDialog != null) nameDialog.Confirm();
        isOpen = false;

        ReleaseBottle();
        if (station != null) station.ShowOnlyInProgress(); // 닫으면 작업대에는 꾸미는 중인 병만 남는다
        station = null;

        root.SetActive(false);
        UIFocus.Close();
    }

    /// <summary>
    /// 병을 완성했다(CompleteBottleButton이 부른다). 먼저 이름을 짓는 상자를 띄운다. (병 그림은 그대로 보이고, 꾸미기 도구는 숨는다)
    /// 이름이 정해지면 FinishCompleted가 병을 병 인벤토리에 넣는다.
    /// </summary>
    public void OnBottleCompleted()
    {
        if (station == null || contents == null || !contents.IsCompleted || namingBottle != null) return;

        namingBottle = contents;
        editTools.SetActive(false);
        string defaultName = namingBottle.DisplayName + " #" + BottleStore.NextArchiveNumber();
        nameDialog.Show(defaultName, FinishCompleted);
    }

    /// <summary>이름이 정해졌다. 완성한 병을 병 인벤토리에 넣고, 작업대의 병은 비워서 같은 종류를 또 만들 수 있게 한 뒤 병 만들기 화면으로 돌아간다.</summary>
    private void FinishCompleted(string givenName)
    {
        BottleContents done = namingBottle;
        namingBottle = null;
        if (done == null || station == null) return;

        string typeName = done.DisplayName;
        ReleaseBottle();
        string id = BottleStore.Archive(done.BottleId, givenName);
        done.ResetToEmpty();
        station.ShowOnlyInProgress();
        ShowChooser(id != null ? "병을 완성했어요! 내 병에 넣었어요 (" + BottleStore.NameOf(id, typeName) + ")" : null);
    }

    /// <summary>병 만들기 화면을 보인다. (보고 있던 병은 놓고, 병 그림과 꾸미기 도구는 숨긴다)</summary>
    private void ShowChooser(string notice = null)
    {
        if (station == null) return;
        ReleaseBottle();

        inventoryView.gameObject.SetActive(false);
        chooser.gameObject.SetActive(true);
        chooser.Refresh(station, notice);
        bottleView.SetActive(false);
        editTools.SetActive(false);
    }

    /// <summary>내 병(병 인벤토리) 화면을 보인다.</summary>
    private void ShowInventory()
    {
        if (station == null) return;
        chooser.gameObject.SetActive(false);
        inventoryView.gameObject.SetActive(true);
        inventoryView.Open(station);
    }

    /// <summary>병 카드를 눌렀다. 지금 고른 흙의 양으로 병을 만들고(이때 정해진다), 그 병을 연다.</summary>
    private void OnBottleChosen(int index)
    {
        if (station == null) return;
        if (BottleChooser.IsRetired(index) || !BottleChooser.IsUnlocked(index)) return;
        BottleContents bottle = station.Get(index);
        if (!bottle.IsCreated)
        {
            // 시작할 때 받은 공짜 병을 먼저 쓰고, 없으면 병 가격만큼 코인을 낸다. 낸 것은 병 기록에 적어 둔다(취소하면 돌려준다).
            bool usedFree = false;
            int paid = 0;
            if (GameState.FreeBottles > 0)
            {
                GameState.UseFreeBottle();
                usedFree = true;
            }
            else if (chooser.RequirePurchase)
            {
                paid = chooser.PriceOf(index);
                if (!GameState.TrySpend(paid))
                {
                    chooser.Refresh(station, "코인이 모자라요 (" + paid + "코인 필요, 지금 " + GameState.Coins + "코인)");
                    return;
                }
            }
            bottle.Create(chooser.SelectedSoil, paid, usedFree);
        }
        ShowBottle(index);
    }

    /// <summary>번호의 병을 작업대에 올리고 패널에 보여 준다.</summary>
    private void ShowBottle(int index)
    {
        if (station == null) return;
        ReleaseBottle();
        chooser.gameObject.SetActive(false);
        inventoryView.gameObject.SetActive(false);
        bottleView.SetActive(true);
        editTools.SetActive(true);

        station.Select(index);
        contents = station.Get(index);
        cancelAsked = false;
        RefreshCancelLabel();
        bottleCamera = contents.ViewCamera;

        dragRotate.SetTarget(contents.GetComponent<BottleViewer>());
        decorator.SetTarget(contents, bottleCamera);
        groundSelector.SetTarget(contents);
        completeButton.SetTarget(contents);
        if (bottleCamera != null) bottleCamera.enabled = true;
    }

    // ───────── 만들다가 취소 (병 버리기) ─────────

    private bool cancelAsked;

    private void RefreshCancelLabel()
    {
        if (cancelLabel == null) return;
        cancelLabel.text = cancelAsked ? "정말 버릴까요?\n(낸 코인은 돌려받아요)" : "병 버리기";
        if (cancelButton != null) cancelButton.gameObject.SetActive(contents != null && !contents.IsCompleted);
    }

    /// <summary>"병 버리기"를 눌렀다. 한 번 더 누르면 지금 꾸미는 병을 없애고 낸 코인(또는 공짜 병)과 병 속 재료를 돌려받는다.</summary>
    private void OnCancelClicked()
    {
        if (contents == null || contents.IsCompleted || station == null) return;
        if (!cancelAsked)
        {
            cancelAsked = true;
            RefreshCancelLabel();
            return;
        }

        string bottleId = contents.BottleId;
        BottleRecord record = BottleStore.Get(bottleId);
        int index = station.IndexOf(bottleId);

        // 병 속 재료와 바닥재를 가방으로 돌려준다. 가방이 가득 차면 우체통으로 보낸다.
        int toBag = 0, toMail = 0;
        var returns = new System.Collections.Generic.List<MaterialData>();
        foreach (PlacedData placed in record.items) if (placed.material != null) returns.Add(placed.material);
        if (record.ground != null) returns.Add(record.ground);
        foreach (MaterialData m in returns)
        {
            if (bag != null && bag.TryAdd(m)) toBag++;
            else { GameState.AddArrivedItem(m); toMail++; }
        }

        // 낸 것을 돌려준다: 공짜 병이면 공짜 병을, 코인을 냈으면 그 코인을. (예전에 만든 병은 기록이 없어서 지금 가격으로 돌려준다)
        string refund;
        if (record.usedFree)
        {
            GameState.AddFreeBottle();
            refund = "공짜 병 1개를 돌려받았어요";
        }
        else
        {
            int coins = record.paidCoins > 0 ? record.paidCoins : (index >= 0 ? chooser.PriceOf(index) : 0);
            GameState.AddCoins(coins);
            refund = coins > 0 ? "코인 " + coins + "개를 돌려받았어요" : "병을 버렸어요";
        }

        string name = contents.DisplayName;
        contents.ResetToEmpty();
        cancelAsked = false;
        GameAudio.Play(GameAudio.Sfx.Remove, 0.8f);
        string notice = name + "을(를) 버렸어요. " + refund;
        if (toBag > 0) notice += " (재료 " + toBag + "개는 가방으로";
        if (toMail > 0) notice += (toBag > 0 ? ", " : " (재료 ") + toMail + "개는 가방이 가득 차서 우체통으로";
        if (toBag > 0 || toMail > 0) notice += ")";
        ShowChooser(notice);
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
}
