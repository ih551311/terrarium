using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Esc로 여는 일시정지 메뉴. 소리 크기, 음악 켜기/끄기, 메인 화면으로, 게임 끝내기.
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

    private bool isOpen;
    private float savedTimeScale = 1f;

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
        musicButton.onClick.AddListener(ToggleMusic);
        menuButton.onClick.AddListener(MenuClicked);
        quitButton.onClick.AddListener(QuitClicked);
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

        if (isOpen) Close();
        else if (!UIFocus.IsOpen && UIFocus.LastCloseFrame != Time.frameCount) Open();
    }

    private void Open()
    {
        if (isOpen) return; // 두 번 열려서 조작이 잠긴 채 남지 않게
        isOpen = true;
        root.SetActive(true);
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
