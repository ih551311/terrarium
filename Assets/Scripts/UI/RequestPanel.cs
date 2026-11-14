using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병 주문 편지 화면. 우체통에서 E를 누르면 와 있는 편지를 보여 주고, 조건에 맞는 완성한 병을 골라 보낼 수 있다.
/// 병을 한 번 누르면 확인 글자로 바뀌고, 한 번 더 누르면 보낸다. 답례(코인과 재료)는 우체통에 도착한다.
/// 게임이 켜질 때 코드로 만들어진다. 편지가 없으면 우체통은 원래대로 상점 화면(MailboxPanel)을 연다.
/// </summary>
public class RequestPanel : MonoBehaviour
{
    private const int MaxRows = 5;

    private static RequestPanel instance;

    private GameObject root;
    private TMP_Text fromLabel, bodyLabel, conditionLabel, noteLabel;
    private readonly List<Button> rows = new List<Button>();
    private readonly List<string> rowBottleIds = new List<string>();
    private string confirmId;       // 한 번 눌러서 확인을 기다리는 병
    private bool done;              // 방금 병을 보내 답장을 보여 주는 중
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("RequestPanel");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RequestPanel>();
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
        Build();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Build()
    {
        Canvas canvas = UIKit.CreateCanvas("RequestCanvas", 350);
        canvas.transform.SetParent(transform, false);
        root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        UIKit.Backdrop(root.transform);
        RectTransform panel = UIKit.Panel(root.transform, "Panel", new Vector2(980f, 860f));

        fromLabel = UIKit.Label(panel, "", 40f, new Vector2(0f, 380f), new Vector2(880f, 60f));
        bodyLabel = UIKit.Label(panel, "", 32f, new Vector2(0f, 270f), new Vector2(860f, 160f), TextAlignmentOptions.TopLeft);
        conditionLabel = UIKit.Label(panel, "", 30f, new Vector2(0f, 150f), new Vector2(860f, 50f), TextAlignmentOptions.Center, new Color(0.45f, 0.30f, 0.12f));

        for (int i = 0; i < MaxRows; i++)
        {
            int index = i;
            Button row = UIKit.MakeButton(panel, "", new Vector2(0f, 70f - i * 82f), new Vector2(820f, 70f), () => RowClicked(index), 28f);
            rows.Add(row);
            rowBottleIds.Add(null);
        }

        noteLabel = UIKit.Label(panel, "", 26f, new Vector2(0f, -318f), new Vector2(880f, 70f), TextAlignmentOptions.Center);
        UIKit.MakeButton(panel, "우체통 열기", new Vector2(-220f, -392f), new Vector2(380f, 64f), OpenMailbox, 28f);
        UIKit.MakeButton(panel, "닫기", new Vector2(220f, -392f), new Vector2(380f, 64f), Close, 28f);
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
        confirmId = null;
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
            bool asked = confirmId == id;
            UIKit.SetButtonText(rows[i], asked
                ? "정말 \"" + NameOf(id) + "\" 병을 보낼까요? (돌아오지 않아요) 한 번 더 누르세요"
                : "\"" + NameOf(id) + "\" 병 보내기" + (BottleStore.IsDisplayed(id) ? "  (전시 중)" : ""));
        }

        if (matching.Count == 0)
            noteLabel.text = BottleStore.ArchivedIds().Count == 0
                ? "아직 완성한 병이 없어요. 천천히 만들어 봐요."
                : "조건에 맞는 병이 아직 없어요. 서두르지 않아도 돼요. (다른 병 " + others + "개)";
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

        if (confirmId != id)
        {
            confirmId = id;
            GameAudio.Play(GameAudio.Sfx.Click, 0.8f);
            Refresh();
            return;
        }

        // 보낸다: 병이 사라지고, 답례가 우체통에 도착한다
        BottleRequest request = BottleRequests.Get(GameState.RequestId);
        if (request == null || !BottleRequests.Matches(GameState.RequestId, BottleStore.Get(id))) { confirmId = null; Refresh(); return; }

        MaterialData reward = null;
        if (!string.IsNullOrEmpty(request.rewardItemId) && MaterialDatabase.Instance != null)
            reward = MaterialDatabase.Instance.Find(request.rewardItemId);

        BottleStore.Remove(id);
        GameState.CompleteRequest(request.coins, reward);
        GameAudio.Play(GameAudio.Sfx.Chime, 0.9f);

        done = true;
        confirmId = null;
        foreach (Button b in rows) b.gameObject.SetActive(false);
        fromLabel.text = "답장 - " + request.from;
        bodyLabel.text = request.thanks;
        conditionLabel.text = "답례: 코인 " + request.coins + (reward != null ? ", " + reward.displayName + " 1개" : "") + " (우체통에 도착했어요)";
        noteLabel.text = "";
    }
}
