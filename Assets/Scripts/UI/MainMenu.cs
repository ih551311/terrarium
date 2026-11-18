using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임을 켜면 처음 나오는 메인 화면 (MainMenu 씬). 시작하기, 도움말, 설정 세 가지가 있다.
/// 화면은 모두 코드로 만든다(UIKit): 어두운 청회색 바탕에 반딧불이 떠다니고, 오른쪽에는 흙 위에 이번에 고른 재료 그림이 놓인 유리병이 있다.
/// 시작하기를 누르면 로딩 화면(LoadingScreen)을 거쳐 섬(Island)으로 간다. 저장 파일이 있으면 GameRoot가 저절로 이어서 불러온다.
/// 설정은 PlayerPrefs에 기억한다: 소리 크기(master_volume, 일시정지 메뉴와 같은 값), 마우스 감도(look_sensitivity), 음악(GameAudio.MusicOn).
/// </summary>
public class MainMenu : MonoBehaviour
{
    public const string GameTitle = "유리병 정원";
    private const string FirstScene = "Island";
    private const string VolumeKey = "master_volume";

    /// <summary>마우스 감도 배율을 기억하는 PlayerPrefs 이름 (FirstPersonController가 읽는다)</summary>
    public const string LookKey = "look_sensitivity";
    private const float LookMin = 0.4f, LookMax = 2f;

    private static readonly Color Cream = new Color(0.96f, 0.92f, 0.82f, 1f);
    private static readonly Color Mist = new Color(0.72f, 0.80f, 0.78f, 1f);
    private static readonly Color Brown = new Color(0.55f, 0.40f, 0.28f, 1f);

    private RectTransform root, jar, jarShadow;
    private CanvasGroup curtain;
    private Button startButton, helpButton, settingsButton;
    private GameObject helpPanel, settingsPanel, openPanel;
    private Button musicButton, screenButton;
    private bool starting;
    private bool fullScreen;
    private float jarBaseY;

    private sealed class Firefly
    {
        public RectTransform rect; public Image image;
        public Vector2 center, amp; public float speed, phase, alpha;
    }
    private readonly List<Firefly> fireflies = new List<Firefly>();
    private readonly List<RectTransform> sprouts = new List<RectTransform>();
    private readonly List<float> sproutPhase = new List<float>();

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        fullScreen = Screen.fullScreen;
        Build();
    }

    private void Start()
    {
        UIFocus.Open(); // 커서를 풀고, 일시정지 메뉴(Esc)가 열리지 않게 한다
        StartCoroutine(FadeIn());
    }

    // ───────── 화면 만들기 ─────────

    private void Build()
    {
        Canvas canvas = UIKit.CreateCanvas("MainMenuCanvas", 100);
        canvas.transform.SetParent(transform, false); // 씬을 떠나면 함께 사라진다
        root = UIKit.Rect(canvas.transform, "Root");
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        BuildBackdrop();
        BuildJar();
        BuildFireflies();
        BuildLeftColumn();

        helpPanel = BuildHelp();
        settingsPanel = BuildSettings();

        // 처음에 까만 막이 걷히며 나타난다
        RectTransform black = UIKit.Rect(root, "Curtain");
        black.anchorMin = Vector2.zero; black.anchorMax = Vector2.one;
        black.offsetMin = black.offsetMax = Vector2.zero;
        Image blackImage = black.gameObject.AddComponent<Image>();
        blackImage.color = new Color(0.07f, 0.09f, 0.11f, 1f);
        blackImage.raycastTarget = false;
        curtain = black.gameObject.AddComponent<CanvasGroup>();
        curtain.alpha = 1f; curtain.blocksRaycasts = false;
    }

    private void BuildBackdrop()
    {
        var gradient = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color top = new Color(0.09f, 0.14f, 0.20f), bottom = new Color(0.25f, 0.32f, 0.31f);
        for (int y = 0; y < 256; y++) gradient.SetPixel(0, y, Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / 255f)));
        gradient.Apply();

        RectTransform rect = UIKit.Rect(root, "Backdrop");
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.texture = gradient;
        image.raycastTarget = false;
    }

    private void BuildJar()
    {
        Sprite glow = Sprite.Create(MakeGlow(), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);

        jarBaseY = -10f;
        jarShadow = UIKit.Rect(root, "JarShadow");
        jarShadow.anchoredPosition = new Vector2(430f, jarBaseY - 322f);
        jarShadow.sizeDelta = new Vector2(560f, 70f);
        Image shadow = jarShadow.gameObject.AddComponent<Image>();
        shadow.sprite = glow; shadow.color = new Color(0f, 0f, 0f, 0.42f); shadow.raycastTarget = false;

        jar = UIKit.Rect(root, "Jar");
        jar.anchoredPosition = new Vector2(430f, jarBaseY);
        jar.sizeDelta = new Vector2(512f, 640f);

        AddImage(jar, "Soil", MakeSoil());

        // 이번에 보여 줄 식물 세 가지. 서로 겹치지 않게 한 줄로 고르게 세운다. 열 때마다 다른 식물이 나온다.
        List<Sprite> icons = PickIcons(3);
        float[] xs = { 132f, 256f, 380f };
        float[] baseY = { 112f, 112f, 112f };
        float[] sizes = { 190f, 190f, 190f };
        for (int i = 0; i < icons.Count; i++)
        {
            RectTransform r = UIKit.Rect(jar, "Plant" + i);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(sizes[i], sizes[i]);
            r.anchoredPosition = new Vector2(xs[i] - 256f, baseY[i] - 320f);
            Image img = r.gameObject.AddComponent<Image>();
            img.sprite = icons[i]; img.preserveAspect = true; img.raycastTarget = false;
            sprouts.Add(r);
            sproutPhase.Add(Random.value * 6.28f);
        }

        AddImage(jar, "Glass", MakeGlass());
    }

    private static void AddImage(RectTransform parent, string name, Texture2D texture)
    {
        RectTransform r = UIKit.Rect(parent, name);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        img.raycastTarget = false;
    }

    private static List<Sprite> PickIcons(int count)
    {
        var pool = new List<Sprite>();
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db != null && db.materials != null)
            foreach (MaterialData m in db.materials)
                // 곧게 서는 식물만 (조각이나 열매 같은 것은 눕거나 작아 보인다)
                if (m != null && m.icon != null && m.kind == MaterialKind.Plant && m.zone != MaterialZone.Shop && m.acquire != AcquireMethod.Shop) pool.Add(m.icon);
        var picked = new List<Sprite>();
        while (picked.Count < count && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            picked.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return picked;
    }

    private void BuildFireflies()
    {
        Sprite glow = Sprite.Create(MakeGlow(), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        for (int i = 0; i < 26; i++)
        {
            RectTransform r = UIKit.Rect(root, "Firefly");
            float size = Random.Range(12f, 34f);
            r.sizeDelta = new Vector2(size, size);
            Image img = r.gameObject.AddComponent<Image>();
            img.sprite = glow; img.raycastTarget = false;
            img.color = new Color(1f, 0.93f, 0.62f, 0f);
            fireflies.Add(new Firefly
            {
                rect = r, image = img,
                center = new Vector2(Random.Range(-900f, 900f), Random.Range(-460f, 460f)),
                amp = new Vector2(Random.Range(30f, 120f), Random.Range(20f, 80f)),
                speed = Random.Range(0.15f, 0.45f), phase = Random.Range(0f, 100f), alpha = Random.Range(0.5f, 1f)
            });
        }
    }

    private void BuildLeftColumn()
    {
        const float x = -480f;
        UIKit.Label(root, GameTitle, 118f, new Vector2(x, 290f), new Vector2(820f, 160f), TextAlignmentOptions.Center, Cream);
        UIKit.Label(root, "작은 병 안에 섬의 풀과 꽃을 담아요", 34f, new Vector2(x, 190f), new Vector2(820f, 56f), TextAlignmentOptions.Center, Mist);

        startButton = UIKit.MakeButton(root, "시작하기", new Vector2(x, 60f), new Vector2(440f, 88f), OnStart, 40f);
        helpButton = UIKit.MakeButton(root, "도움말", new Vector2(x, -50f), new Vector2(440f, 88f), () => OpenPanel(helpPanel), 40f);
        settingsButton = UIKit.MakeButton(root, "설정", new Vector2(x, -160f), new Vector2(440f, 88f), () => OpenPanel(settingsPanel), 40f);

        string note = null;
        if (SaveSystem.LoadFailed) note = "저장 파일을 읽지 못해서 새로 시작해요";
        else if (SaveSystem.HasSave) note = "저장된 정원이 있어요 (" + GameState.Day + "일째)";
        if (note != null) UIKit.Label(root, note, 27f, new Vector2(x, -255f), new Vector2(820f, 44f), TextAlignmentOptions.Center, Mist);
    }

    // ───────── 도움말 ─────────

    private static readonly string[,] MoveKeys =
    {
        { "W A S D", "걷기" },
        { "Shift", "달리기" },
        { "Space", "점프" },
        { "마우스", "둘러보기" },
        { "E", "줍기, 열기, 말 걸기" },
        { "좌클릭", "나무, 돌, 풀 캐기" },
        { "Tab", "가방 열기" },
        { "M", "지도 (눌러서 이동)" },
        { "N", "작은 지도 켜기/끄기" },
        { "P", "음악 켜기/끄기" },
        { "Esc", "메뉴, 닫기" },
    };

    private static readonly string[,] BottleKeys =
    {
        { "클릭", "고른 재료 놓기" },
        { "우클릭", "놓은 재료 빼기" },
        { "휠", "재료 돌리기" },
        { "Q / R", "재료 작게 / 크게" },
        { "끌기", "병 돌려 보기" },
    };

    private const string HowToPlay =
        "휠, Q, R은 병 속 재료에 마우스를 올리고 써요.\n\n" +
        "섬에서 재료를 모아 집 작업대(E)에서 병을 꾸며요. 완성한 병은 선반에 올려 둘 수 있어요.\n\n" +
        "우체통에 재료를 보내면 다음 날 코인이 돼요. 코인으로 장식을 사고, 도구를 강화하고, 선착장에서 다른 섬을 열어요.\n\n" +
        "침대에서 자면 다음 날이 되고 저장돼요.";

    private GameObject BuildHelp()
    {
        GameObject holder = NewPanel("HelpPanel", "도움말", new Vector2(1260f, 960f), out RectTransform panel);

        UIKit.Label(panel, "움직이기와 줍기", 38f, new Vector2(-265f, 345f), new Vector2(560f, 50f), TextAlignmentOptions.Left, Brown);
        UIKit.Label(panel, "병 꾸미기", 38f, new Vector2(310f, 345f), new Vector2(560f, 50f), TextAlignmentOptions.Left, Brown);

        for (int i = 0; i < MoveKeys.GetLength(0); i++)
            KeyRow(panel, -460f, -185f, 280f - i * 58f, MoveKeys[i, 0], MoveKeys[i, 1]);
        for (int i = 0; i < BottleKeys.GetLength(0); i++)
            KeyRow(panel, 115f, 390f, 280f - i * 58f, BottleKeys[i, 0], BottleKeys[i, 1]);

        UIKit.Label(panel, HowToPlay, 25f, new Vector2(310f, -185f), new Vector2(560f, 340f), TextAlignmentOptions.TopLeft);
        return holder;
    }

    private static void KeyRow(RectTransform panel, float keyX, float descX, float y, string key, string desc)
    {
        UIKit.Label(panel, key, 30f, new Vector2(keyX, y), new Vector2(170f, 52f), TextAlignmentOptions.Left, Brown);
        UIKit.Label(panel, desc, 27f, new Vector2(descX, y), new Vector2(360f, 52f), TextAlignmentOptions.Left);
    }

    // ───────── 설정 ─────────

    private GameObject BuildSettings()
    {
        GameObject holder = NewPanel("SettingsPanel", "설정", new Vector2(820f, 760f), out RectTransform panel);

        // 소리 크기
        UIKit.Label(panel, "소리 크기", 32f, new Vector2(-230f, 190f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        TMP_Text volumeValue = UIKit.Label(panel, "", 28f, new Vector2(345f, 190f), new Vector2(100f, 50f), TextAlignmentOptions.Right, Brown);
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        volumeValue.text = Mathf.RoundToInt(volume * 100f) + "%";
        UIKit.MakeSlider(panel, new Vector2(100f, 190f), new Vector2(320f, 40f), volume, v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(VolumeKey, v);
            volumeValue.text = Mathf.RoundToInt(v * 100f) + "%";
        });

        // 마우스 감도 (배율. 100%가 기본)
        UIKit.Label(panel, "마우스 감도", 32f, new Vector2(-230f, 70f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        TMP_Text lookValue = UIKit.Label(panel, "", 28f, new Vector2(345f, 70f), new Vector2(100f, 50f), TextAlignmentOptions.Right, Brown);
        float look = Mathf.Clamp(PlayerPrefs.GetFloat(LookKey, 1f), LookMin, LookMax);
        lookValue.text = Mathf.RoundToInt(look * 100f) + "%";
        UIKit.MakeSlider(panel, new Vector2(100f, 70f), new Vector2(320f, 40f), Mathf.InverseLerp(LookMin, LookMax, look), v =>
        {
            float scale = Mathf.Round(Mathf.Lerp(LookMin, LookMax, v) * 20f) / 20f; // 5% 단위
            PlayerPrefs.SetFloat(LookKey, scale);
            lookValue.text = Mathf.RoundToInt(scale * 100f) + "%";
        });

        // 배경 음악
        UIKit.Label(panel, "배경 음악", 32f, new Vector2(-230f, -50f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        musicButton = UIKit.MakeButton(panel, "", new Vector2(130f, -50f), new Vector2(260f, 64f), () =>
        {
            GameAudio.MusicOn = !GameAudio.MusicOn;
            RefreshSettingTexts();
        });

        // 화면
        UIKit.Label(panel, "화면", 32f, new Vector2(-230f, -170f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        screenButton = UIKit.MakeButton(panel, "", new Vector2(130f, -170f), new Vector2(260f, 64f), () =>
        {
            fullScreen = !fullScreen;
            Screen.fullScreenMode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            RefreshSettingTexts();
        });

        RefreshSettingTexts();
        return holder;
    }

    private void RefreshSettingTexts()
    {
        if (musicButton != null) UIKit.SetButtonText(musicButton, GameAudio.MusicOn ? "켜짐" : "꺼짐");
        if (screenButton != null) UIKit.SetButtonText(screenButton, fullScreen ? "전체 화면" : "창 모드");
    }

    // ───────── 패널 열고 닫기 ─────────

    private GameObject NewPanel(string name, string title, Vector2 size, out RectTransform panel)
    {
        RectTransform holder = UIKit.Rect(root, name);
        holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one;
        holder.offsetMin = holder.offsetMax = Vector2.zero;

        // 어두운 막을 누르면 닫힌다
        RectTransform back = UIKit.Backdrop(holder);
        Button backButton = back.gameObject.AddComponent<Button>();
        backButton.transition = Selectable.Transition.None;
        backButton.onClick.AddListener(ClosePanel);

        panel = UIKit.Panel(holder, "Panel", size);
        UIKit.Label(panel, title, 60f, new Vector2(0f, size.y / 2f - 75f), new Vector2(size.x - 100f, 84f));
        UIKit.MakeButton(panel, "닫기", new Vector2(0f, -size.y / 2f + 65f), new Vector2(300f, 70f), ClosePanel);
        holder.gameObject.SetActive(false);
        return holder.gameObject;
    }

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
        startButton.interactable = helpButton.interactable = settingsButton.interactable = false;
        GameAudio.Play(GameAudio.Sfx.Chime, 0.5f);

        // 이 화면은 씬과 함께 사라지므로, 씬을 넘어가는 일은 남는 오브젝트에게 맡긴다
        GameObject host = new GameObject("MainMenuTravel");
        DontDestroyOnLoad(host);
        host.AddComponent<Travel>().Begin(FirstScene);
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

        float t = Time.unscaledTime;

        // 병이 둥둥 떠 있듯 아주 천천히 오르내리고, 그림자는 그에 맞춰 살짝 변한다
        float bob = Mathf.Sin(t * 0.8f);
        jar.anchoredPosition = new Vector2(430f, jarBaseY + bob * 6f);
        jarShadow.localScale = new Vector3(1f - bob * 0.03f, 1f - bob * 0.05f, 1f);

        // 병 속 재료는 바람에 흔들리듯 살랑거린다
        for (int i = 0; i < sprouts.Count; i++)
            sprouts[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f + sproutPhase[i]) * 2.2f);

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

    // ───────── 그림 만들기 (파일 없이 코드로 그린다) ─────────

    // 병 모양 거리: 음수면 병 안쪽 (몸통과 목을 부드럽게 이은 모양, y는 위쪽이 큼)
    private static float JarDist(Vector2 p)
    {
        float body = Box(p, new Vector2(256f, 285f), new Vector2(205f, 245f), 80f);
        float neck = Box(p, new Vector2(256f, 545f), new Vector2(112f, 60f), 22f);
        return SmoothMin(body, neck, 36f);
    }

    private static float Box(Vector2 p, Vector2 center, Vector2 half, float radius)
    {
        float qx = Mathf.Abs(p.x - center.x) - half.x + radius;
        float qy = Mathf.Abs(p.y - center.y) - half.y + radius;
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return Mathf.Min(Mathf.Max(qx, qy), 0f) + outside - radius;
    }

    private static float SmoothMin(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    private static Texture2D MakeGlass()
    {
        const int W = 512, H = 640;
        var pixels = new Color32[W * H];
        Color glass = new Color(0.86f, 0.95f, 1f);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = JarDist(p);

                float a = 0f;
                if (d < 0f)
                {
                    float inside = Mathf.Clamp01(0.5f - d);
                    a = (0.09f + 0.20f * Mathf.Clamp01(1f + d / 34f)) * inside;
                }
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d) / 3f) * 0.55f;
                a = 1f - (1f - a) * (1f - rim);

                float h1 = Box(p, new Vector2(92f, 300f), new Vector2(8f, 120f), 8f);
                float h2 = Box(p, new Vector2(92f, 458f), new Vector2(8f, 8f), 8f);
                float h3 = Box(p, new Vector2(425f, 255f), new Vector2(4f, 70f), 4f);
                a = Mathf.Max(a, 0.34f * Mathf.Clamp01(0.5f - h1));
                a = Mathf.Max(a, 0.34f * Mathf.Clamp01(0.5f - h2));
                a = Mathf.Max(a, 0.17f * Mathf.Clamp01(0.5f - h3));

                // 코르크 뚜껑
                float ld = Box(p, new Vector2(256f, 612f), new Vector2(142f, 26f), 11f);
                float cover = Mathf.Clamp01(0.5f - ld);
                Color lid = y < 597 ? new Color(0.46f, 0.32f, 0.21f) : (y > 628 ? new Color(0.72f, 0.54f, 0.37f) : new Color(0.62f, 0.45f, 0.30f));

                float outA = cover + a * (1f - cover);
                Color rgb = outA > 0f ? (lid * cover + glass * (a * (1f - cover))) / outA : glass;
                pixels[y * W + x] = new Color(rgb.r, rgb.g, rgb.b, outA);
            }
        }
        return Finish(W, H, pixels);
    }

    private static Texture2D MakeSoil()
    {
        const int W = 512, H = 640;
        var pixels = new Color32[W * H];
        for (int y = 0; y < 260; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = JarDist(p);
                float top = 118f + 7f * Mathf.Sin(x * 0.035f) + 4f * Mathf.Sin(x * 0.09f + 1f);
                float cover = Mathf.Clamp01(top - y + 0.5f) * Mathf.Clamp01(-d - 6f + 0.5f);
                if (cover <= 0f) continue;

                float grain = (Hash(x / 3, y / 3) - 0.5f) * 0.06f;
                float depth = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(y / top));
                Color c = new Color(0.34f, 0.25f, 0.19f) * depth;
                if (top - y < 7f) c = Color.Lerp(c, new Color(0.47f, 0.36f, 0.27f), 0.8f);
                c += new Color(grain, grain, grain);
                pixels[y * W + x] = new Color(c.r, c.g, c.b, 0.96f * cover);
            }
        }
        return Finish(W, H, pixels);
    }

    private static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    private static Texture2D Finish(int w, int h, Color32[] pixels)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // 가운데가 밝고 가장자리가 부드럽게 사라지는 동그라미 (반딧불이, 그림자)
    private static Texture2D MakeGlow()
    {
        const int S = 64;
        var pixels = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float r = new Vector2(x + 0.5f - S / 2f, y + 0.5f - S / 2f).magnitude / (S / 2f);
                float a = Mathf.Clamp01(1f - r);
                a *= a;
                pixels[y * S + x] = new Color(1f, 1f, 1f, a);
            }
        }
        return Finish(S, S, pixels);
    }
}
