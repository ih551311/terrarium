using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼. 게임을 처음 시작하면 화면 왼쪽 위에 작은 쪽지가 떠서 해야 할 일을 한 가지씩 알려 준다.
/// 걷기 → 줍기 → 캐기 → 가방 → 집 → 병 만들기 → 선반에 올리기 → 우체통 → 잠자기. 하나를 해내면 "좋아요!"가 뜨고 다음으로 넘어간다.
/// - 단계는 저장에 남는다 (GameState.TutorialStep). 튜토리얼이 생기기 전에 시작한 저장은 건너뛴다.
/// - F1을 누르면 튜토리얼을 끈다. 메뉴의 "새로 시작"으로 저장을 지우면 다시 처음부터 나온다.
/// - 시작 편지를 닫은 뒤부터 나오고, 가방, 작업대 같은 화면이 열려 있는 동안과 자는 동안에는 숨는다.
/// 화면은 Assets/Resources/UI/TutorialGuide.prefab (유니티에서 열어 모양을 고칠 수 있다). 글자는 아래 Steps 표에서 고친다.
/// </summary>
public class TutorialGuide : MonoBehaviour
{
    public const int Done = 99;

    private struct Step
    {
        public string title, body;
        public Step(string title, string body) { this.title = title; this.body = body; }
    }

    private static readonly Step[] Steps =
    {
        new Step("걸어 보세요", "W A S D로 걷고, 마우스로 둘러봐요.\nShift를 누르면 달려요."),
        new Step("재료 줍기", "바닥에 놓인 재료를 바라보고\nE를 눌러 주워요."),
        new Step("도구로 캐기", "풀과 꽃은 괭이, 나무는 도끼, 돌은 곡괭이로\n마우스 왼쪽을 눌러 캐요.\n재료를 3개 더 모아 보세요."),
        new Step("가방 보기", "Tab을 눌러 모은 재료를 확인해요."),
        new Step("집으로", "집 문을 바라보고 E로 열어서\n안으로 들어가요."),
        new Step("병 만들기", "작업대를 바라보고 E를 눌러요.\n병을 고르고 재료를 놓은 뒤\n'완성'을 눌러 이름을 지어 주세요."),
        new Step("선반에 올리기", "Tab을 누르고 병 탭에서\n병을 눌러 '선반에 올리기'를 해요."),
        new Step("우체통", "남는 재료는 문 앞 우체통에서 E로 팔아요.\n다음 날 아침에 코인이 와요."),
        new Step("잠자기", "해가 지면 침대에서 E로 자요.\n자고 나면 새로운 하루가 시작돼요."),
    };

    private static TutorialGuide instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText, bodyText, footerText;
    [SerializeField] private Image cardBackground;

    private int baselineStep = -1;
    private int baseItems, baseCompleted, baseDisplayed, baseOutbox, baseDay;
    private float walked;
    private Vector3 lastPos;
    private bool hasLastPos;
    private float flashUntil = -1f, finishUntil = -1f;
    private Color normalColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject prefab = Resources.Load<GameObject>("UI/TutorialGuide");
        if (prefab == null) { Debug.LogError("UI/TutorialGuide 프리팹이 없어요. 메뉴 '유리병 정원 > 길잡이 화면 프리팹 만들기'를 눌러 주세요."); return; }
        GameObject go = Instantiate(prefab);
        go.name = "TutorialGuide";
        DontDestroyOnLoad(go);
        instance = go.GetComponent<TutorialGuide>();
    }

    private void Awake()
    {
        normalColor = cardBackground.color;
        root.SetActive(false);
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        GameRoot game = GameRoot.Instance;
        Transform player = game != null ? game.Player : null;
        int step = GameState.TutorialStep;

        bool active = player != null && step < Steps.Length;
        bool finishing = finishUntil > 0f && Time.unscaledTime < finishUntil;
        if (!active && !finishing) { root.SetActive(false); hasLastPos = false; return; }

        // 걸은 거리 (순간 이동은 세지 않는다)
        if (player != null)
        {
            Vector3 p = player.position; p.y = 0f;
            if (hasLastPos) { float d = Vector3.Distance(p, lastPos); if (d < 3f) walked += d; }
            lastPos = p; hasLastPos = true;
        }

        if (finishing)
        {
            Show("튜토리얼을 마쳤어요!", "이제 마음대로 정원을 꾸며 보세요.\n도움말은 메인 화면과 Esc 메뉴에 있어요.", string.Empty, true);
            return;
        }

        // 시작 편지를 읽고 닫은 뒤에, 다른 화면이 없을 때만 보인다
        bool visible = GameState.LetterRead && !LetterPanel.IsOpen && !UIFocus.IsOpen && !SleepSequence.Sleeping;
        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame && visible)
        {
            GameState.TutorialStep = Done;
            root.SetActive(false);
            return;
        }

        if (baselineStep != step) CaptureBaselines(step, player);

        if (flashUntil > 0f)
        {
            if (Time.unscaledTime < flashUntil) { if (visible) Show("좋아요!", string.Empty, string.Empty, true); else root.SetActive(false); return; }
            flashUntil = -1f;
            GameState.TutorialStep = step + 1;
            if (GameState.TutorialStep >= Steps.Length) { GameState.TutorialStep = Done; finishUntil = Time.unscaledTime + 5f; }
            return;
        }

        if (StepDone(step, player)) { flashUntil = Time.unscaledTime + 1.3f; GameAudio.Play(GameAudio.Sfx.Coin, 0.5f); }

        if (visible) Show(Steps[step].title, Steps[step].body, (step + 1) + " / " + Steps.Length + "     F1 튜토리얼 끄기", false);
        else root.SetActive(false);
    }

    private void Show(string title, string body, string footer, bool good)
    {
        root.SetActive(true);
        titleText.text = title;
        bodyText.text = body;
        footerText.text = footer;
        cardBackground.color = good ? new Color(0.78f, 0.9f, 0.74f, 0.97f) : normalColor;
    }

    private static int TotalItems(Transform player)
    {
        Bag bag = player != null ? player.GetComponent<Bag>() : null;
        if (bag == null) return 0;
        int total = 0;
        foreach (Bag.Slot s in bag.Slots) total += s.count;
        return total;
    }

    private static int DisplayedCount()
    {
        int n = 0;
        foreach (string id in BottleStore.ArchivedIds()) if (BottleStore.IsDisplayed(id)) n++;
        return n;
    }

    private void CaptureBaselines(int step, Transform player)
    {
        baselineStep = step;
        walked = 0f;
        baseItems = TotalItems(player);
        baseCompleted = BottleStore.CompletedCount;
        baseDisplayed = DisplayedCount();
        baseOutbox = GameState.Outbox.Count + GameState.Orders.Count;
        baseDay = GameState.Day;
    }

    private bool StepDone(int step, Transform player)
    {
        switch (step)
        {
            case 0: return walked >= 6f;
            case 1: return TotalItems(player) > baseItems;
            case 2: return TotalItems(player) >= baseItems + 3;
            case 3: return InventoryPanel.IsOpen;
            case 4: return IndoorZone.Inside;
            case 5: return BottleStore.CompletedCount > baseCompleted;
            case 6: return DisplayedCount() > baseDisplayed;
            case 7: return GameState.Outbox.Count + GameState.Orders.Count > baseOutbox;
            case 8: return GameState.Day > baseDay;
        }
        return false;
    }
}