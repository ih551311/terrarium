using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임을 켜면 처음 나오는 메인 화면 (MainMenu 씬). 시작하기, 도움말, 설정, 게임 종료가 있고, 저장이 있으면 왼쪽 아래에 "새로 시작"도 있다.
/// 화면(캔버스)은 MainMenu 씬 안에 실제 오브젝트로 들어 있다: 어두운 청회색 바탕, 떠다니는 반딧불이, 흙 위에 식물이 놓인 유리병,
/// 왼쪽의 제목과 버튼, 도움말과 설정 판. 모양을 바꾸려면 씬에서 직접 고치거나, 메뉴 "유리병 정원 > 메인 화면 만들기"로 다시 만든다(UIBake).
/// 이 스크립트는 버튼을 연결하고, 병 속 식물을 무작위로 고르고, 반딧불이를 움직인다.
/// 시작하기를 누르면 로딩 화면(LoadingScreen)을 거쳐 섬(Island)으로 간다. 저장 파일이 있으면 GameRoot가 저절로 이어서 불러온다.
/// 설정은 PlayerPrefs에 기억한다: 소리 크기(master_volume, 일시정지 메뉴와 같은 값), 마우스 감도(look_sensitivity), 음악(GameAudio.MusicOn).
/// </summary>
public class MainMenu : MonoBehaviour
{
    public const string GameTitle = "Mini Terra";
    private const string FirstScene = "Island";
    private const string VolumeKey = "master_volume";

    /// <summary>마우스 감도 배율을 기억하는 PlayerPrefs 이름 (FirstPersonController가 읽는다)</summary>
    public const string LookKey = "look_sensitivity";
    public const float LookMin = 0.4f, LookMax = 2f;

    [Header("버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button helpButton, settingsButton;
    [Tooltip("게임 끝내기")]
    [SerializeField] private Button quitButton;
    [Tooltip("저장을 지우고 처음부터 (저장이 있을 때만 보인다, 두 번 눌러야 지워진다)")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private TMP_Text noteLabel;

    [Header("판 (도움말, 설정)")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("판을 닫는 버튼들 (닫기 버튼과 어두운 막)")]
    [SerializeField] private Button[] closeButtons;

    [Header("설정")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValue;
    [SerializeField] private Slider lookSlider;
    [SerializeField] private TMP_Text lookValue;
    [SerializeField] private Button musicButton, screenButton, qualityButton;

    [Header("그림")]
    [SerializeField] private CanvasGroup curtain;
    [SerializeField] private RectTransform jar, jarShadow;
    [Tooltip("병 속 식물 자리 (열 때마다 다른 식물 그림이 들어간다)")]
    [SerializeField] private Image[] plantSlots;
    [SerializeField] private Image[] fireflyImages;

    private GameObject openPanel;
    private bool starting;
    private bool fullScreen;
    private float jarBaseX, jarBaseY;
    private float newGameArmedUntil;
    private string savedNote;

    private sealed class Firefly
    {
        public RectTransform rect; public Image image;
        public Vector2 center, amp; public float speed, phase, alpha;
    }
    private readonly List<Firefly> fireflies = new List<Firefly>();
    private float[] sproutPhase;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        fullScreen = Screen.fullScreen;
        curtain.alpha = 1f; // 까만 막으로 시작해서 걷힌다 (씬에서는 투명하게 두어 편집이 가려지지 않는다)
        Wire();
        PlantSprouts();
        ScatterFireflies();
        helpPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void Start()
    {
        UIFocus.Open(); // 커서를 풀고, 일시정지 메뉴(Esc)가 열리지 않게 한다
        StartCoroutine(FadeIn());
    }

    // ───────── 연결 ─────────

    private void Wire()
    {
        startButton.onClick.AddListener(OnStart);
        helpButton.onClick.AddListener(() => OpenPanel(helpPanel));
        settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
        foreach (Button b in closeButtons) b.onClick.AddListener(ClosePanel);
        Canvas menuCanvas = settingsButton.GetComponentInParent<Canvas>();
        if (menuCanvas != null) CreditsPanel.AddCornerButton(settingsButton, menuCanvas.transform).transform.SetSiblingIndex(1);

        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (newGameButton != null)
        {
            newGameButton.gameObject.SetActive(SaveSystem.HasSave);
            newGameButton.onClick.AddListener(OnNewGame);
        }
        if (qualityButton != null)
            qualityButton.onClick.AddListener(() => { PerformanceDefaults.CycleQuality(); RefreshSettingTexts(); });

        string note = null;
        if (SaveSystem.LoadFailed) note = "저장 파일을 읽지 못해서 새로 시작해요";
        else if (SaveSystem.HasSave) note = "저장된 정원이 있어요 (" + GameState.Day + "일째)";
        noteLabel.text = note ?? string.Empty;
        savedNote = noteLabel.text;

        // 소리 크기
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        volumeSlider.SetValueWithoutNotify(volume);
        volumeValue.text = Mathf.RoundToInt(volume * 100f) + "%";
        volumeSlider.onValueChanged.AddListener(v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
            volumeValue.text = Mathf.RoundToInt(v * 100f) + "%";
        });

        // 마우스 감도 (배율. 100%가 기본)
        float look = Mathf.Clamp(PlayerPrefs.GetFloat(LookKey, 1f), LookMin, LookMax);
        lookSlider.SetValueWithoutNotify(Mathf.InverseLerp(LookMin, LookMax, look));
        lookValue.text = Mathf.RoundToInt(look * 100f) + "%";
        lookSlider.onValueChanged.AddListener(v =>
        {
            float scale = Mathf.Round(Mathf.Lerp(LookMin, LookMax, v) * 20f) / 20f; // 5% 단위
            PlayerPrefs.SetFloat(LookKey, scale);
            lookValue.text = Mathf.RoundToInt(scale * 100f) + "%";
        });

        musicButton.onClick.AddListener(() =>
        {
            GameAudio.MusicOn = !GameAudio.MusicOn;
            RefreshSettingTexts();
        });
        screenButton.onClick.AddListener(() =>
        {
            fullScreen = !fullScreen;
            Screen.fullScreenMode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            RefreshSettingTexts();
        });
        RefreshSettingTexts();

        jarBaseX = jar.anchoredPosition.x;
        jarBaseY = jar.anchoredPosition.y;
    }

    // 이번에 보여 줄 식물 그림을 고른다. 곧게 서는 식물만 (조각이나 열매 같은 것은 눕거나 작아 보인다).
    private void PlantSprouts()
    {
        var pool = new List<Sprite>();
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db != null && db.materials != null)
            foreach (MaterialData m in db.materials)
                if (m != null && m.icon != null && m.kind == MaterialKind.Plant && m.zone != MaterialZone.Shop && m.acquire != AcquireMethod.Shop) pool.Add(m.icon);

        sproutPhase = new float[plantSlots.Length];
        for (int i = 0; i < plantSlots.Length; i++)
        {
            sproutPhase[i] = Random.value * 6.28f;
            if (pool.Count == 0) { plantSlots[i].gameObject.SetActive(false); continue; }
            int pick = Random.Range(0, pool.Count);
            plantSlots[i].sprite = pool[pick];
            plantSlots[i].color = Color.white;
            pool.RemoveAt(pick);
        }
    }

    private void ScatterFireflies()
    {
        foreach (Image img in fireflyImages)
        {
            fireflies.Add(new Firefly
            {
                rect = img.rectTransform, image = img,
                center = new Vector2(Random.Range(-900f, 900f), Random.Range(-460f, 460f)),
                amp = new Vector2(Random.Range(30f, 120f), Random.Range(20f, 80f)),
                speed = Random.Range(0.15f, 0.45f), phase = Random.Range(0f, 100f), alpha = Random.Range(0.5f, 1f)
            });
        }
    }

    private void RefreshSettingTexts()
    {
        UIKit.SetButtonText(musicButton, GameAudio.MusicOn ? "켜짐" : "꺼짐");
        UIKit.SetButtonText(screenButton, fullScreen ? "전체 화면" : "창 모드");
        if (qualityButton != null) UIKit.SetButtonText(qualityButton, PerformanceDefaults.QualityLabel());
    }

    // ───────── 패널 열고 닫기 ─────────

    private void OpenPanel(GameObject panel)
    {
        if (starting) return;
        ClosePanel();
        openPanel = panel;
        panel.SetActive(true);
        RefreshSettingTexts();
    }

    private void ClosePanel()
    {
        if (openPanel == null) return;
        openPanel.SetActive(false);
        openPanel = null;
        PlayerPrefs.Save();
    }

    // ───────── 시작하기 ─────────

    private void OnStart()
    {
        if (starting) return;
        starting = true;
        ClosePanel();
        SetMenuInteractable(false);
        GameAudio.Play(GameAudio.Sfx.Chime, 0.5f);

        // 이 화면은 씬과 함께 사라지므로, 씬을 넘어가는 일은 남는 오브젝트에게 맡긴다
        GameObject host = new GameObject("MainMenuTravel");
        DontDestroyOnLoad(host);
        host.AddComponent<Travel>().Begin(FirstScene);
    }

    private void SetMenuInteractable(bool on)
    {
        startButton.interactable = helpButton.interactable = settingsButton.interactable = on;
        if (quitButton != null) quitButton.interactable = on;
        if (newGameButton != null) newGameButton.interactable = on;
    }

    // ───────── 게임 끝내기, 새로 시작 ─────────

    private void OnQuit()
    {
        if (starting) return;
        starting = true;
        SetMenuInteractable(false);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>저장을 지우는 버튼. 실수로 누르지 않게 4초 안에 한 번 더 눌러야 지워진다.</summary>
    private void OnNewGame()
    {
        if (starting) return;
        if (Time.unscaledTime > newGameArmedUntil)
        {
            newGameArmedUntil = Time.unscaledTime + 4f;
            UIKit.SetButtonText(newGameButton, "지울까요?");
            noteLabel.text = "저장된 정원이 지워져요. 지우려면 한 번 더 눌러 주세요";
            return;
        }
        starting = true;
        SetMenuInteractable(false);
        SaveSystem.DeleteSave();
        noteLabel.text = "저장을 지웠어요. 게임을 다시 켜면 처음부터 시작해요";
        StartCoroutine(QuitAfterDelay());
    }

    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private sealed class Travel : MonoBehaviour
    {
        public void Begin(string sceneName) { StartCoroutine(Run(sceneName)); }

        private IEnumerator Run(string sceneName)
        {
            yield return LoadingScreen.Show(sceneName);
            float shownAt = Time.realtimeSinceStartup;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            while (Time.realtimeSinceStartup - shownAt < LoadingScreen.MinSeconds) yield return null;
            UIFocus.Close(); // 걷기와 커서 잠금이 돌아온다
            yield return LoadingScreen.Hide();
            Destroy(gameObject);
        }
    }

    // ───────── 매 프레임 ─────────

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.unscaledDeltaTime;
            curtain.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / 0.8f);
            yield return null;
        }
        curtain.alpha = 0f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ClosePanel();

        // 새로 시작 버튼을 한 번 누르고 가만히 두면 원래대로 돌아간다
        if (newGameArmedUntil > 0f && !starting && Time.unscaledTime > newGameArmedUntil)
        {
            newGameArmedUntil = 0f;
            UIKit.SetButtonText(newGameButton, "새로 시작");
            noteLabel.text = savedNote;
        }

        float t = Time.unscaledTime;

        // 병이 둥둥 떠 있듯 아주 천천히 오르내리고, 그림자는 그에 맞춰 살짝 변한다
        float bob = Mathf.Sin(t * 0.8f);
        jar.anchoredPosition = new Vector2(jarBaseX, jarBaseY + bob * 6f);
        jarShadow.localScale = new Vector3(1f - bob * 0.03f, 1f - bob * 0.05f, 1f);

        // 병 속 식물은 바람에 흔들리듯 살랑거린다
        for (int i = 0; i < plantSlots.Length; i++)
            plantSlots[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f + sproutPhase[i]) * 2.2f);

        // 반딧불이
        foreach (Firefly f in fireflies)
        {
            float u = t * f.speed + f.phase;
            f.rect.anchoredPosition = f.center + new Vector2(Mathf.Sin(u) * f.amp.x, Mathf.Sin(u * 1.37f + 1.1f) * f.amp.y);
            float blink = 0.5f + 0.5f * Mathf.Sin(t * (0.6f + f.speed * 2f) + f.phase * 3f);
            Color c = f.image.color; c.a = f.alpha * blink * blink;
            f.image.color = c;
        }
    }
}
