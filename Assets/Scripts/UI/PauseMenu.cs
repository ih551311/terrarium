using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Esc로 여는 일시정지 메뉴. 첫 화면에는 "소리 설정", "화면 설정", 메인 화면으로, 게임 끝내기만 있고,
/// 소리 설정(소리, 음악, 효과음 크기, 음악 켜기/끄기)과 화면 설정(마우스 감도, 화면 모드, 해상도, 그래픽)은 각각 따로 연다.
/// 열려 있는 동안 게임 시간이 멈춘다. 다른 패널이 열려 있으면 열리지 않는다.
/// 화면은 Assets/Resources/UI/PauseMenu.prefab (캔버스)에 있고, 게임이 켜질 때 하나 꺼내 씬을 넘어 남겨 둔다.
/// 화면을 다시 만들려면 메뉴 "유리병 정원 > 화면 캔버스 다시 만들기" (UIBake).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private const string VolumeKey = "master_volume";

    private static PauseMenu instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button musicButton, menuButton, quitButton;
    [Header("설정 (메인 화면 설정과 같은 값)")]
    [SerializeField] private Slider lookSlider, musicSlider, sfxSlider;
    [SerializeField] private Button screenButton, resolutionButton, qualityButton;

    [Header("화면 묶음 (첫 화면, 소리 설정, 화면 설정)")]
    [SerializeField] private GameObject mainGroup, soundGroup, screenGroup;
    [SerializeField] private Button soundOpenButton, screenOpenButton, soundBackButton, screenBackButton;

    private bool isOpen;
    private float savedTimeScale = 1f;

    private void ShowGroup(GameObject group)
    {
        if (mainGroup != null) mainGroup.SetActive(group == mainGroup);
        if (soundGroup != null) soundGroup.SetActive(group == soundGroup);
        if (screenGroup != null) screenGroup.SetActive(group == screenGroup);
    }

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject prefab = Resources.Load<GameObject>("UI/PauseMenu");
        if (prefab == null) { Debug.LogError("UI/PauseMenu 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요."); return; }
        GameObject go = Instantiate(prefab);
        go.name = "PauseMenu";
        DontDestroyOnLoad(go);
        instance = go.GetComponent<PauseMenu>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        volumeSlider.onValueChanged.AddListener(v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
        });
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(GameAudio.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => GameAudio.MusicVolume = v);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => { GameAudio.SfxVolume = v; GameAudio.Play(GameAudio.Sfx.Click, 0.6f); });
        }
        if (lookSlider != null)
        {
            float look = Mathf.Clamp(PlayerPrefs.GetFloat(MainMenu.LookKey, 1f), MainMenu.LookMin, MainMenu.LookMax);
            lookSlider.SetValueWithoutNotify(Mathf.InverseLerp(MainMenu.LookMin, MainMenu.LookMax, look));
            lookSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(MainMenu.LookKey, Mathf.Round(Mathf.Lerp(MainMenu.LookMin, MainMenu.LookMax, v) * 20f) / 20f));
        }
        if (screenButton != null) screenButton.onClick.AddListener(() => { DisplaySettings.ToggleFullScreen(); StartCoroutine(RefreshAfterScreenChange()); });
        if (resolutionButton != null) resolutionButton.onClick.AddListener(() => { DisplaySettings.CycleResolution(); StartCoroutine(RefreshAfterScreenChange()); });
        if (qualityButton != null) qualityButton.onClick.AddListener(() => { PerformanceDefaults.CycleQuality(); RefreshTexts(); });
        musicButton.onClick.AddListener(ToggleMusic);
        menuButton.onClick.AddListener(MenuClicked);
        quitButton.onClick.AddListener(QuitClicked);
        if (soundOpenButton != null) soundOpenButton.onClick.AddListener(() => ShowGroup(soundGroup));
        if (screenOpenButton != null) screenOpenButton.onClick.AddListener(() => ShowGroup(screenGroup));
        if (soundBackButton != null) soundBackButton.onClick.AddListener(() => ShowGroup(mainGroup));
        if (screenBackButton != null) screenBackButton.onClick.AddListener(() => ShowGroup(mainGroup));
        CreditsPanel.AddButtonBelow(quitButton, 16f);
        ShowGroup(mainGroup);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
        if (CreditsPanel.IsOpen || CreditsPanel.ClosedFrame == Time.frameCount || CollectionBook.IsOpen || CollectionBook.ClosedFrame == Time.frameCount) return; // 크레딧이 위에 열려 있으면(또는 방금 닫혔으면) 그쪽이 먼저다

        if (isOpen)
        {
            if (mainGroup != null && !mainGroup.activeSelf) ShowGroup(mainGroup); // 설정 화면에서는 Esc로 첫 화면으로 돌아간다
            else Close();
        }
        else if (!UIFocus.IsOpen && UIFocus.LastCloseFrame != Time.frameCount) Open();
    }

    private void Open()
    {
        if (isOpen) return; // 두 번 열려서 조작이 잠긴 채 남지 않게
        isOpen = true;
        root.SetActive(true);
        ShowGroup(mainGroup);
        RefreshTexts();
        savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        UIFocus.Open();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.6f);
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        root.SetActive(false);
        Time.timeScale = savedTimeScale;
        UIFocus.Close();
    }

    private void RefreshTexts()
    {
        UIKit.SetButtonText(musicButton, GameAudio.MusicOn ? "음악 끄기" : "음악 켜기");
        if (screenButton != null) UIKit.SetButtonText(screenButton, "화면: " + DisplaySettings.ScreenLabel());
        if (resolutionButton != null)
        {
            UIKit.SetButtonText(resolutionButton, "해상도: " + DisplaySettings.ResolutionLabel());
            resolutionButton.interactable = !DisplaySettings.FullScreen;
        }
        if (qualityButton != null) UIKit.SetButtonText(qualityButton, "그래픽: " + PerformanceDefaults.QualityLabel());
    }

    // 화면 크기는 바뀐 다음 프레임에 적용되므로 글자는 잠시 뒤에 다시 쓴다 (메뉴가 멈춘 시간 안에서도 돌도록 실제 시간으로 기다린다)
    private System.Collections.IEnumerator RefreshAfterScreenChange()
    {
        yield return new WaitForSecondsRealtime(0.15f);
        RefreshTexts();
    }

    // 저장은 메인 화면으로 갈 때와 끝낼 때 저절로 한다 (GameRoot)
    private void MenuClicked()
    {
        GameRoot gameRoot = GameRoot.Instance;
        Close(); // 시간과 커서를 되돌려 놓는다
        if (gameRoot != null) gameRoot.ReturnToMenu();
    }

    private void ToggleMusic()
    {
        GameAudio.MusicOn = !GameAudio.MusicOn;
        RefreshTexts();
    }

    private void QuitClicked()
    {
        // 끝내기 전에 시간을 되돌려 놓는다 (에디터에서는 Play가 멈추기 전에 남지 않게)
        Time.timeScale = savedTimeScale;
        if (GameRoot.Instance != null) GameRoot.Instance.SaveNow();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
