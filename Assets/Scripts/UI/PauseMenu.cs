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
        private Button musicButton;
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
        UIKit.Backdrop(root.transform);
        RectTransform panel = UIKit.Panel(root.transform, "Panel", new Vector2(640f, 560f));

        UIKit.Label(panel, "잠시 쉬어가요", 48f, new Vector2(0f, 205f), new Vector2(560f, 70f));

        UIKit.Label(panel, "소리 크기", 28f, new Vector2(0f, 110f), new Vector2(460f, 40f));
        UIKit.MakeSlider(panel, new Vector2(0f, 65f), new Vector2(420f, 40f), AudioListener.volume, v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
        });

        musicButton = UIKit.MakeButton(panel, "", new Vector2(0f, -15f), new Vector2(460f, 72f), ToggleMusic);
        UIKit.MakeButton(panel, "메인 화면으로", new Vector2(0f, -105f), new Vector2(460f, 72f), MenuClicked);
        UIKit.MakeButton(panel, "게임 끝내기", new Vector2(0f, -195f), new Vector2(460f, 72f), QuitClicked);
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
