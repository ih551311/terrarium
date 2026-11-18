using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Esc로 여는 일시정지 메뉴. 이어하기, 지금 저장하기, 소리 크기, 음악 켜기/끄기, 게임 끝내기.
/// 열려 있는 동안 게임 시간이 멈춘다. 게임이 켜질 때 코드로 만들어지고, 다른 패널이 열려 있으면 열리지 않는다.
/// (게임을 끄거나 앱이 멈출 때도 저절로 저장된다 - GameRoot)
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private const string VolumeKey = "master_volume";

    private static PauseMenu instance;

    private GameObject root;
    private TMP_Text status;
    private Button musicButton, quitButton, menuButton;
    private bool quitAsked, menuAsked;
    private bool isOpen;
    private float savedTimeScale = 1f;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PauseMenu>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        Build();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Build()
    {
        Canvas canvas = UIKit.CreateCanvas("PauseMenuCanvas", 400);
        canvas.transform.SetParent(transform, false);
        root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        UIKit.Backdrop(root.transform);
        RectTransform panel = UIKit.Panel(root.transform, "Panel", new Vector2(640f, 740f));

        UIKit.Label(panel, "잠시 쉬어가요", 48f, new Vector2(0f, 300f), new Vector2(560f, 70f));
        UIKit.MakeButton(panel, "이어서 하기", new Vector2(0f, 210f), new Vector2(460f, 72f), Close);
        UIKit.MakeButton(panel, "지금 저장하기", new Vector2(0f, 120f), new Vector2(460f, 72f), SaveClicked);

        UIKit.Label(panel, "소리 크기", 28f, new Vector2(0f, 40f), new Vector2(460f, 40f));
        UIKit.MakeSlider(panel, new Vector2(0f, -5f), new Vector2(420f, 40f), AudioListener.volume, v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
        });

        musicButton = UIKit.MakeButton(panel, "", new Vector2(0f, -85f), new Vector2(460f, 72f), ToggleMusic);
        menuButton = UIKit.MakeButton(panel, "메인 화면으로", new Vector2(0f, -175f), new Vector2(460f, 72f), MenuClicked);
        quitButton = UIKit.MakeButton(panel, "게임 끝내기", new Vector2(0f, -265f), new Vector2(460f, 72f), QuitClicked);
        status = UIKit.Label(panel, "", 26f, new Vector2(0f, -335f), new Vector2(560f, 40f));
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
        isOpen = true;
        quitAsked = false;
        menuAsked = false;
        root.SetActive(true);
        RefreshTexts();
        status.text = "";
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
        UIKit.SetButtonText(quitButton, quitAsked ? "정말 끝낼까요? 한 번 더 누르세요" : "게임 끝내기");
        UIKit.SetButtonText(menuButton, menuAsked ? "돌아갈까요? 한 번 더 누르세요" : "메인 화면으로");
    }

    private void MenuClicked()
    {
        if (!menuAsked)
        {
            menuAsked = true;
            quitAsked = false;
            RefreshTexts();
            status.text = "돌아가기 전에 저장해요";
            return;
        }
        GameRoot root = GameRoot.Instance;
        Close(); // 시간과 커서를 되돌려 놓는다
        if (root != null) root.ReturnToMenu();
    }

    private void SaveClicked()
    {
        bool ok = GameRoot.Instance != null && GameRoot.Instance.SaveNow();
        status.text = ok ? "저장했어요" : "저장하지 못했어요";
        GameAudio.Play(GameAudio.Sfx.Click, 0.8f);
    }

    private void ToggleMusic()
    {
        GameAudio.MusicOn = !GameAudio.MusicOn;
        RefreshTexts();
    }

    private void QuitClicked()
    {
        if (!quitAsked)
        {
            quitAsked = true;
            RefreshTexts();
            status.text = "끝낼 때 저절로 저장돼요";
            return;
        }
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
