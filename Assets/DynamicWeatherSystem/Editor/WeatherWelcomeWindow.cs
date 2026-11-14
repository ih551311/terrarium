using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DynamicWeatherSystem.Editor
{
    /// <summary>
    /// Welcome window: quick start, what's new, documentation and other assets by the publisher.
    /// Layout: header, sidebar navigation, page content, and a small footer with the review button.
    ///
    /// Opens automatically once per package version (never on every editor start),
    /// and can be reopened from Tools > Dynamic Weather System > Welcome.
    /// </summary>
    public class WeatherWelcomeWindow : EditorWindow
    {
        // ------------------------------------------------------------------
        // Package info and links. Update these before each release.
        // ------------------------------------------------------------------
        public const string Version = "1.1.0";

        private const string ReviewUrl =
            "https://assetstore.unity.com/packages/tools/particles-effects/urp-dynamic-weather-system-370650#reviews";
        private const string PublisherUrl =
            "https://assetstore.unity.com/publishers/118971";

        // ------------------------------------------------------------------

        private const string PrefLastShownVersion = "DynamicWeatherSystem.Welcome.LastShownVersion";
        private const string PrefShowOnUpdate     = "DynamicWeatherSystem.Welcome.ShowOnUpdate";
        private const string MenuPath             = "Tools/Dynamic Weather System/";

        private enum Page { QuickStart, WhatsNew, Documentation }

        private static readonly string[] PageLabels =
            { "Quick Start", "What's New", "Documentation" };

        private const float HeaderHeight  = 58f;
        private const float FooterHeight  = 30f;
        private const float SidebarWidth  = 150f;
        private const float ContentMargin = 18f;

        [SerializeField] private Page _page = Page.QuickStart;
        private Vector2 _scroll;

        // NonSerialized: otherwise Unity restores half-initialized styles after a domain reload.
        [System.NonSerialized] private GUIStyle _title, _version, _badge, _pageTitle, _heading, _body, _mono,
                                                _card, _tab, _tabSelected, _stepNumber, _footerLabel,
                                                _footerLabelRight, _smallButton;
        [System.NonSerialized] private bool _stylesReady;

        // --- Colors (adapt to the editor skin) ---
        private static bool Dark => EditorGUIUtility.isProSkin;
        private static Color HeaderBg   => Dark ? new Color(0.10f, 0.13f, 0.20f) : new Color(0.20f, 0.30f, 0.46f);
        private static Color SidebarBg  => Dark ? new Color(0.16f, 0.16f, 0.17f) : new Color(0.80f, 0.80f, 0.81f);
        private static Color FooterBg   => Dark ? new Color(0.14f, 0.14f, 0.15f) : new Color(0.78f, 0.78f, 0.79f);
        private static Color Separator  => Dark ? new Color(0.08f, 0.08f, 0.09f) : new Color(0.60f, 0.60f, 0.62f);
        private static Color Accent     => new Color(0.30f, 0.62f, 1.00f);
        private static Color TabHover   => Dark ? new Color(1f, 1f, 1f, 0.04f) : new Color(0f, 0f, 0f, 0.05f);
        private static Color TabActive  => Dark ? new Color(0.30f, 0.62f, 1f, 0.16f) : new Color(0.30f, 0.62f, 1f, 0.20f);

        // ------------------------------------------------------------------
        // Menu items and auto-open
        // ------------------------------------------------------------------

        [MenuItem(MenuPath + "Welcome", false, 0)]
        public static void Open() => Open(Page.QuickStart);

        private static void Open(Page page)
        {
            var w = GetWindow<WeatherWelcomeWindow>(true, "Dynamic Weather System", true);
            w.minSize = new Vector2(640f, 440f);
            w.maxSize = new Vector2(900f, 720f);
            w._page   = page;
            w._scroll = Vector2.zero;
            w.Show();
        }

        [MenuItem(MenuPath + "Documentation", false, 1)]
        private static void OpenDocumentation() => OpenDoc("README.md");

        [MenuItem(MenuPath + "Leave a Review", false, 40)]
        private static void OpenReview() => Application.OpenURL(ReviewUrl);

        [InitializeOnLoadMethod]
        private static void ShowOnceAfterImportOrUpdate()
        {
            if (Application.isBatchMode) return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!EditorPrefs.GetBool(PrefShowOnUpdate, true)) return;

                string lastShown = EditorPrefs.GetString(PrefLastShownVersion, "");
                if (lastShown == Version) return;

                EditorPrefs.SetString(PrefLastShownVersion, Version);

                // First import → Quick Start. Update from an older version → What's New.
                Open(string.IsNullOrEmpty(lastShown) ? Page.QuickStart : Page.WhatsNew);
            };
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        private void OnEnable()
        {
            wantsMouseMove = true;   // needed for the sidebar hover highlight
        }

        private void OnGUI()
        {
            InitStyles();

            if ((int)_page < 0 || (int)_page >= PageLabels.Length) _page = Page.QuickStart;

            var full   = new Rect(0, 0, position.width, position.height);
            var header  = new Rect(0, 0, full.width, HeaderHeight);
            var footer  = new Rect(0, full.height - FooterHeight, full.width, FooterHeight);
            var sidebar = new Rect(0, header.yMax, SidebarWidth, footer.y - header.yMax);
            var content = new Rect(sidebar.xMax, header.yMax, full.width - SidebarWidth, sidebar.height);

            DrawHeader(header);
            DrawSidebar(sidebar);
            DrawContent(content);
            DrawFooter(footer);
        }

        private void DrawHeader(Rect r)
        {
            EditorGUI.DrawRect(r, HeaderBg);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), Accent);

            GUI.Label(new Rect(r.x + 18, r.y + 10, r.width - 140, 24), "URP Dynamic Weather System", _title);
            GUI.Label(new Rect(r.x + 18, r.y + 32, r.width - 140, 18),
                "Lightweight, modular weather for the Universal Render Pipeline", _version);

            var badge = new Rect(r.xMax - 82, r.y + 18, 64, 20);
            EditorGUI.DrawRect(badge, new Color(1f, 1f, 1f, 0.12f));
            GUI.Label(badge, "v" + Version, _badge);
        }

        private void DrawSidebar(Rect r)
        {
            EditorGUI.DrawRect(r, SidebarBg);
            EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), Separator);

            float y = r.y + 12;
            for (int i = 0; i < PageLabels.Length; i++)
            {
                var tabRect  = new Rect(r.x, y, r.width - 1, 32);
                bool selected = (int)_page == i;

                if (selected)
                {
                    EditorGUI.DrawRect(tabRect, TabActive);
                    EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.y, 3, tabRect.height), Accent);
                }
                else if (tabRect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(tabRect, TabHover);
                }

                EditorGUIUtility.AddCursorRect(tabRect, MouseCursor.Link);

                if (GUI.Button(tabRect, PageLabels[i], selected ? _tabSelected : _tab))
                {
                    _page   = (Page)i;
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }

                y += 34;
            }

            // "More Assets" is a link, not a page: it opens the publisher page directly.
            y += 6;
            EditorGUI.DrawRect(new Rect(r.x + 12, y, r.width - 25, 1), Separator);
            y += 7;
            var linkRect = new Rect(r.x, y, r.width - 1, 32);
            if (linkRect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(linkRect, TabHover);
            EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);
            if (GUI.Button(linkRect, new GUIContent("More Assets  ↗", "Open the publisher page on the Asset Store"), _tab))
                Application.OpenURL(PublisherUrl);

            // Repaint on hover so the highlight follows the mouse.
            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        private void DrawContent(Rect r)
        {
            var inner = new Rect(r.x + ContentMargin, r.y + 12,
                                 r.width - ContentMargin * 2, r.height - 16);

            GUILayout.BeginArea(inner);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_page)
            {
                case Page.QuickStart:    DrawQuickStart();    break;
                case Page.WhatsNew:      DrawWhatsNew();      break;
                case Page.Documentation: DrawDocumentation(); break;
            }

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawFooter(Rect r)
        {
            EditorGUI.DrawRect(r, FooterBg);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), Separator);

            bool show    = EditorPrefs.GetBool(PrefShowOnUpdate, true);
            bool newShow = EditorGUI.ToggleLeft(new Rect(r.x + 12, r.y + 6, 240, 18),
                                                "Show this window after updates", show, _footerLabel);
            if (newShow != show) EditorPrefs.SetBool(PrefShowOnUpdate, newShow);

            var reviewRect = new Rect(r.xMax - 132, r.y + 5, 120, 20);
            var saved = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.80f, 0.30f);
            if (GUI.Button(reviewRect, "★ Leave a Review", _smallButton))
                Application.OpenURL(ReviewUrl);
            GUI.backgroundColor = saved;
            EditorGUIUtility.AddCursorRect(reviewRect, MouseCursor.Link);

            GUI.Label(new Rect(reviewRect.x - 190, r.y + 7, 182, 18),
                      "Enjoying it? It really helps!", _footerLabelRight);
        }

        // ------------------------------------------------------------------
        // Pages
        // ------------------------------------------------------------------

        private void DrawQuickStart()
        {
            PageTitle("Quick Start", "Weather in your scene in under a minute.");

            StepCard(1, "Create the weather system",
                "Adds a WeatherSystem with every module, ready to use.",
                () =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Create Weather System", GUILayout.Height(26)))
                            WeatherSystemSetup.CreateWeatherSystem(null, withCycle: false);
                        if (GUILayout.Button("Create with Automatic Cycle", GUILayout.Height(26)))
                            WeatherSystemSetup.CreateWeatherSystem(null, withCycle: true);
                    }
                });

            StepCard(2, "Press Play and try it",
                "Select WeatherSystem and use Play Mode Preview in the Inspector to switch presets " +
                "(Clear, Rain, Fog, Storm, Snow).", null);

            StepCard(3, "Change the weather from code",
                null,
                () =>
                {
                    GUILayout.Label(
                        "public WeatherManager weather;\n" +
                        "public WeatherStateData storm;\n\n" +
                        "weather.SetWeather(storm, 5f);   // 5 second transition", _mono);
                });

            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Demo Scene", GUILayout.Height(24))) OpenDemoScene();
                if (GUILayout.Button("Full Quick Start Guide", GUILayout.Height(24))) OpenDoc("QuickStart.md");
            }
        }

        private void DrawWhatsNew()
        {
            PageTitle("What's New in " + Version, "Free update — thanks for using the Weather System!");

            Section("New", new[]
            {
                "Snow Module and a new Snow preset",
                "Lightning Module with flashes and thunder (no audio files needed)",
                "Wind Module: drives a WindZone and tilts rain and snow",
                "Weather Cycle: automatic weather changes, no code",
                "Events: WeatherChangeStarted / WeatherChanged (C# and UnityEvents)",
                "One-click setup and \"Add Missing Modules\" in the Inspector",
            });

            Section("Fixed", new[]
            {
                "Rain is now properly transparent (color alpha works)",
                "Quieter Console: transition logs are off by default",
                "Optional ambient light control on the Light Module",
            });

            using (new EditorGUILayout.VerticalScope(_card))
            {
                GUILayout.Label("Updating an existing scene?", _heading);
                GUILayout.Label("Your scenes and weather states keep working as before. " +
                                "To get Snow, Wind and Lightning, add the missing modules:", _body);
                GUILayout.Space(4);

                var manager = Object.FindAnyObjectByType<WeatherManager>();
                using (new EditorGUI.DisabledScope(manager == null || Application.isPlaying))
                {
                    int missing = manager != null ? WeatherSystemSetup.GetMissingModules(manager).Count : 0;
                    string label = manager == null ? "No WeatherManager in the open scene"
                                 : missing == 0     ? "All modules already added ✓"
                                 : "Add " + missing + " Missing Module(s) to '" + manager.name + "'";

                    using (new EditorGUI.DisabledScope(missing == 0))
                    {
                        if (GUILayout.Button(label, GUILayout.Height(24)))
                        {
                            WeatherSystemSetup.AddMissingModules(manager);
                            Selection.activeGameObject = manager.gameObject;
                        }
                    }
                }
            }

            GUILayout.Space(4);
            if (GUILayout.Button("Full Changelog", GUILayout.Height(22))) OpenDoc("CHANGELOG.md");
        }

        private void DrawDocumentation()
        {
            PageTitle("Documentation", "Guides and references included in the package.");

            DocRow("Overview & Modules", "Everything the package does, module by module.", "README.md");
            DocRow("Quick Start Guide", "Step-by-step setup, by hand or in one click.", "QuickStart.md");
            DocRow("API Reference", "Every public method, property, event and field.", "API_Reference.md");
            DocRow("Creating Custom Modules", "Extend the system with your own module.", "Custom_Modules.md");
            DocRow("Changelog", "What changed in each version and how to update.", "CHANGELOG.md");

            GUILayout.Space(6);
            using (new EditorGUILayout.VerticalScope(_card))
            {
                GUILayout.Label("Demo Scene", _heading);
                GUILayout.Label("A ready-made scene with every preset and an in-game control panel.", _body);
                GUILayout.Space(2);
                if (GUILayout.Button("Open Demo Scene", GUILayout.Height(24))) OpenDemoScene();
            }
        }

        // ------------------------------------------------------------------
        // Building blocks
        // ------------------------------------------------------------------

        private void PageTitle(string title, string subtitle)
        {
            GUILayout.Label(title, _pageTitle);
            if (!string.IsNullOrEmpty(subtitle))
                GUILayout.Label(subtitle, _body);
            GUILayout.Space(10);
        }

        private void StepCard(int number, string title, string text, System.Action extra)
        {
            using (new EditorGUILayout.HorizontalScope(_card))
            {
                GUILayout.Label(number.ToString(), _stepNumber, GUILayout.Width(26), GUILayout.Height(26));
                GUILayout.Space(6);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(title, _heading);
                    if (!string.IsNullOrEmpty(text)) GUILayout.Label(text, _body);
                    if (extra != null) { GUILayout.Space(4); extra(); }
                }
            }
            GUILayout.Space(6);
        }

        private void Section(string title, string[] items)
        {
            using (new EditorGUILayout.VerticalScope(_card))
            {
                GUILayout.Label(title, _heading);
                GUILayout.Space(2);
                foreach (var item in items)
                    GUILayout.Label("•  " + item, _body);
            }
            GUILayout.Space(6);
        }

        private void DocRow(string title, string description, string file)
        {
            using (new EditorGUILayout.HorizontalScope(_card))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(title, _heading);
                    GUILayout.Label(description, _body);
                }
                if (GUILayout.Button("Open", GUILayout.Width(64), GUILayout.Height(30)))
                    OpenDoc(file);
            }
            GUILayout.Space(4);
        }

        // ------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------

        private static string PackageRoot()
        {
            // Locate the package folder from this script, so it works wherever users move it.
            foreach (var guid in AssetDatabase.FindAssets("WeatherWelcomeWindow t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);   // .../DynamicWeatherSystem/Editor/WeatherWelcomeWindow.cs
                if (!path.EndsWith("/Editor/WeatherWelcomeWindow.cs")) continue;
                return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(path)).Replace("\\", "/");
            }
            return "Assets/DynamicWeatherSystem";
        }

        private static void OpenDoc(string fileName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(PackageRoot() + "/Documentation/" + fileName);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogWarning("[DynamicWeatherSystem] Documentation file not found: " + fileName);
            }
        }

        private static void OpenDemoScene()
        {
            var path = PackageRoot() + "/Example/Scene WeatherSystem.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning("[DynamicWeatherSystem] Demo scene not found at " + path);
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        // ------------------------------------------------------------------
        // Styles
        // ------------------------------------------------------------------

        private void InitStyles()
        {
            if (_stylesReady) return;

            var white = new Color(0.95f, 0.97f, 1f);

            _title = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 17, normal = { textColor = white } };
            _version = new GUIStyle(EditorStyles.label)
                { fontSize = 11, normal = { textColor = new Color(0.78f, 0.85f, 0.95f) } };
            _pageTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            _heading   = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, wordWrap = true };
            _body      = new GUIStyle(EditorStyles.label)     { wordWrap = true };
            _mono      = new GUIStyle(EditorStyles.textArea)
                { wordWrap = false, padding = new RectOffset(8, 8, 6, 6), font = EditorStyles.standardFont };
            _card      = new GUIStyle(EditorStyles.helpBox)   { padding = new RectOffset(10, 10, 8, 8) };

            _tab = new GUIStyle(EditorStyles.label)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(16, 8, 0, 0)
            };
            _tabSelected = new GUIStyle(_tab) { fontStyle = FontStyle.Bold };

            _stepNumber = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Accent }
            };

            _badge = new GUIStyle(_version) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _footerLabel      = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
            _footerLabelRight = new GUIStyle(_footerLabel) { alignment = TextAnchor.MiddleRight };
            _smallButton = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, fixedHeight = 20 };

            _stylesReady = true;
        }
    }
}
