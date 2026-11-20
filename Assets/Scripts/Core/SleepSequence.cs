using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 침대에서 자는 연출 (마인크레프트처럼).
///   1) 화면이 천천히 어두워지고 부드러운 종소리가 난다
///   2) 검은 화면에서 "쿨쿨..." 하고, 그동안 밤이 지나간다 (여기서 GameState.Sleep()과 저장을 한다)
///   3) 아침 소리와 함께 천천히 밝아지고, 가운데에 "N일째 아침"과 오늘의 날씨가 떴다가 사라진다
/// 연출 중에는 UIFocus로 걷기와 줍기가 멈춘다. 이미 자는 중이면 또 시작하지 않는다.
/// 화면은 Assets/Resources/UI/SleepSequence.prefab (유니티에서 열어 글꼴, 색, 자리를 직접 고칠 수 있다)을 처음 쓸 때 꺼내 와 씬을 넘어 남겨 둔다.
/// </summary>
public class SleepSequence : MonoBehaviour
{
    private const float FadeOut = 1.6f;
    private const float Dream = 2.6f;
    private const float FadeIn = 2.4f;
    private const float TitleHold = 2.2f;

    private static SleepSequence instance;

    /// <summary>자는 중이면 true</summary>
    public static bool Sleeping => instance != null && instance.running;

    [SerializeField] private CanvasGroup curtain;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private TMP_Text dreamText, titleText, subText;
    private bool running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    /// <summary>자기 연출을 시작한다. 이미 자는 중이면 아무것도 하지 않는다.</summary>
    public static void Begin(Interactor who)
    {
        if (Sleeping) return;
        if (instance == null) LoadPrefab();
        if (instance == null) return;
        instance.StartCoroutine(instance.Run(who));
    }

    private static void LoadPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("UI/SleepSequence");
        if (prefab == null) { Debug.LogError("UI/SleepSequence 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요."); return; }
        GameObject go = Object.Instantiate(prefab);
        go.name = "SleepSequence";
        Object.DontDestroyOnLoad(go);
        instance = go.GetComponent<SleepSequence>();
    }

    private void Awake()
    {
        curtain.alpha = 0f; curtain.blocksRaycasts = false;
        titleGroup.alpha = 0f; titleGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    private IEnumerator Run(Interactor who)
    {
        running = true;
        UIFocus.Open(); // 걷기와 줍기를 멈춘다
        bool closed = false;
        try
        {
            yield return Sequence(who);
        }
        finally
        {
            // 도중에 오류가 나도 화면이 까맣게 남거나 조작이 잠긴 채 남지 않게 한다
            curtain.alpha = 0f;
            titleGroup.alpha = 0f;
            if (!closed) { closed = true; UIFocus.Close(); }
            running = false;
        }
    }

    private IEnumerator Sequence(Interactor who)
    {
        Bag bag = who != null ? who.Bag : Object.FindFirstObjectByType<Bag>();
        titleGroup.alpha = 0f;
        dreamText.text = string.Empty;

        // 1) 어두워진다
        GameAudio.Play(GameAudio.Sfx.Chime, 0.25f);
        yield return Fade(curtain, 0f, 1f, FadeOut);

        // 2) 검은 화면에서 잔다. 이 사이에 하루가 넘어간다.
        float t = 0f;
        bool slept = false;
        bool mail = false;
        while (t < Dream)
        {
            t += Time.unscaledDeltaTime;
            int dots = 1 + (int)(t * 2.2f) % 3;
            dreamText.text = "쿨쿨" + new string('.', dots);
            dreamText.alpha = Mathf.Clamp01(Mathf.Min(t, Dream - t) * 2f);
            if (!slept && t >= Dream * 0.5f)
            {
                slept = true;
                mail = GameState.Sleep();
                SaveSystem.Save(bag);
            }
            yield return null;
        }
        if (!slept) { mail = GameState.Sleep(); SaveSystem.Save(bag); }
        dreamText.text = string.Empty;

        // 3) 아침이 밝아 온다
        titleText.text = GameState.Day + "일째 아침";
        subText.text = "오늘은 " + Weather.NameOf(Weather.Today);
        GameAudio.Play(GameAudio.Sfx.Chime, 0.35f);
        FirstPersonController.Nod(0.03f); // 눈을 뜨며 고개를 든다
        StartCoroutine(Title());
        yield return Fade(curtain, 1f, 0f, FadeIn);

        string text = GameState.NewRequestToday ? "우체통에 편지가 와 있어요" : mail ? "우체통에 도착한 게 있어요" : null;
        if (text != null && who != null) who.ShowMessage(text);
    }

    private IEnumerator Title()
    {
        yield return Fade(titleGroup, 0f, 1f, 0.7f);
        yield return new WaitForSecondsRealtime(TitleHold);
        yield return Fade(titleGroup, 1f, 0f, 1.0f);
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
            group.alpha = Mathf.Lerp(from, to, u);
            yield return null;
        }
        group.alpha = to;
    }
}
