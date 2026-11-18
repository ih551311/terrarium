using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 주문 편지 화면. 우체통에서 E를 누르면 와 있는 편지를 보여 주고, 조건에 맞는 완성한 병을 골라 보낼 수 있다.
/// 병을 누르면 바로 보낸다. 답례(코인과 재료)는 우체통에 도착한다.
/// 화면은 Assets/Resources/UI/RequestPanel.prefab (캔버스)에 있고 게임이 켜질 때 꺼내 둔다. 편지가 없으면 우체통은 원래대로 상점 화면(MailboxPanel)을 연다.
/// </summary>
public class RequestPanel : MonoBehaviour
{
    /// <summary>한 번에 보여 주는 병 줄 수 (화면에 구워 둔 버튼 수와 같다)</summary>
    public const int MaxRows = 5;

    private static RequestPanel instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text fromLabel, bodyLabel, conditionLabel, noteLabel;
    [SerializeField] private Button[] rowButtons;
    [SerializeField] private Button mailboxButton, closeButton;
    private readonly List<Button> rows = new List<Button>();
    private readonly List<string> rowBottleIds = new List<string>();
    private bool done;              // 방금 병을 보내 답장을 보여 주는 중
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject prefab = Resources.Load<GameObject>("UI/RequestPanel");
        if (prefab == null) { Debug.LogError("UI/RequestPanel 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요."); return; }
        GameObject go = Instantiate(prefab);
        go.name = "RequestPanel";
        DontDestroyOnLoad(go);
        instance = go.GetComponent<RequestPanel>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    /// <summary>편지를 연다. 열 수 없으면(편지가 없음) false.</summary>
    public static bool TryOpen()
    {
        if (instance == null || GameState.RequestId < 0) return false;
        instance.Open();
        return true;
    }

    private void Awake()
    {
        for (int i = 0; i < rowButtons.Length; i++)
        {
            int index = i;
            rowButtons[i].onClick.AddListener(() => RowClicked(index));
            rows.Add(rowButtons[i]);
            rowBottleIds.Add(null);
        }
        mailboxButton.onClick.AddListener(OpenMailbox);
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (isOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;
        done = false;
        root.SetActive(true);
        UIFocus.Open();
        GameState.MarkRequestSeen();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.8f);
        Refresh();
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        root.SetActive(false);
        UIFocus.Close();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.5f);
    }

    private void OpenMailbox()
    {
        Close();
        if (GameRoot.Instance != null && GameRoot.Instance.MailboxPanel != null) GameRoot.Instance.MailboxPanel.Open();
    }

    private void Refresh()
    {
        BottleRequest request = BottleRequests.Get(GameState.RequestId);
        if (request == null && !done) { Close(); return; }

        foreach (Button b in rows) b.gameObject.SetActive(false);
        if (done) return;

        fromLabel.text = "편지 - " + request.from;
        bodyLabel.text = request.body;
        conditionLabel.text = "조건: " + request.condition;

        // 조건에 맞는 병부터 보여 준다
        var matching = new List<string>();
        int others = 0;
        foreach (string id in BottleStore.ArchivedIds())
        {
            if (BottleRequests.Matches(GameState.RequestId, BottleStore.Get(id))) matching.Add(id);
            else others++;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool has = i < matching.Count;
            rows[i].gameObject.SetActive(has);
            rowBottleIds[i] = has ? matching[i] : null;
            if (!has) continue;
            string id = matching[i];
            UIKit.SetButtonText(rows[i], "\"" + NameOf(id) + "\" 병 보내기" + (BottleStore.IsDisplayed(id) ? "  (전시 중)" : ""));
        }

        if (matching.Count == 0)
            noteLabel.text = BottleStore.ArchivedIds().Count == 0
                ? "아직 완성한 병이 없어요."
                : "조건에 맞는 병이 없어요. (가진 병 " + others + "개)";
        else if (matching.Count > MaxRows)
            noteLabel.text = "조건에 맞는 병이 " + matching.Count + "개 있어요. (앞의 " + MaxRows + "개만 보여요)";
        else
            noteLabel.text = "보낸 병은 돌아오지 않지만, 답례가 우체통에 도착해요.";
    }

    private static string NameOf(string id)
    {
        BottleRecord record = BottleStore.Get(id);
        return BottleStore.NameOf(id, record.typeId != null ? record.typeId : "병");
    }

    private void RowClicked(int index)
    {
        string id = rowBottleIds[index];
        if (string.IsNullOrEmpty(id) || done) return;

        // 보낸다: 병이 사라지고, 답례가 우체통에 도착한다
        BottleRequest request = BottleRequests.Get(GameState.RequestId);
        if (request == null || !BottleRequests.Matches(GameState.RequestId, BottleStore.Get(id))) { Refresh(); return; }

        MaterialData reward = null;
        if (!string.IsNullOrEmpty(request.rewardItemId) && MaterialDatabase.Instance != null)
            reward = MaterialDatabase.Instance.Find(request.rewardItemId);

        BottleStore.Remove(id);
        GameState.CompleteRequest(request.coins, reward);
        GameAudio.Play(GameAudio.Sfx.Chime, 0.9f);

        done = true;
        foreach (Button b in rows) b.gameObject.SetActive(false);
        fromLabel.text = "답장 - " + request.from;
        bodyLabel.text = request.thanks;
        conditionLabel.text = "답례: 코인 " + request.coins + (reward != null ? ", " + reward.displayName + " 1개" : "") + " (우체통에 도착했어요)";
        noteLabel.text = "";
    }
}
