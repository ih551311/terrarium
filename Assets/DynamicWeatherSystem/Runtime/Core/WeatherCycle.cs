using System;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Changes the weather automatically over time — no code required.
    ///
    /// Setup:
    ///   1. Add this component to any GameObject (for example the WeatherSystem root).
    ///   2. Fill the Entries list with weather states, their weight and how long they last.
    ///   3. Choose Sequential (in order, looping) or Random (weighted).
    ///
    /// The cycle can be paused, resumed or skipped from code: Pause(), Resume(), Next().
    /// Calling WeatherManager.SetWeather() yourself does not stop the cycle; call Pause()
    /// first if you want manual control (for example during a cutscene).
    /// </summary>
    [AddComponentMenu("Dynamic Weather System/Weather Cycle")]
    public class WeatherCycle : MonoBehaviour
    {
        public enum CycleMode
        {
            /// <summary>Plays entries in list order and loops.</summary>
            Sequential,
            /// <summary>Picks the next entry at random using each entry's weight.</summary>
            Random
        }

        [Serializable]
        public class Entry
        {
            [Tooltip("Weather state to apply.")]
            public WeatherStateData state;

            [Tooltip("Relative chance of being picked in Random mode. Ignored in Sequential mode.")]
            [Min(0f)] public float weight = 1f;

            [Tooltip("Minimum time (seconds) this weather lasts before the next change.")]
            [Min(1f)] public float minDuration = 60f;

            [Tooltip("Maximum time (seconds) this weather lasts before the next change.")]
            [Min(1f)] public float maxDuration = 120f;
        }

        [Tooltip("WeatherManager to control. Auto-found if left empty.")]
        [SerializeField] private WeatherManager weatherManager;

        [SerializeField] private CycleMode mode = CycleMode.Random;

        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Tooltip("Transition duration in seconds between states.")]
        [SerializeField, Min(0f)] private float transitionDuration = 8f;

        [Tooltip("Start the cycle automatically when the scene starts.")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("In Random mode, avoid picking the same state twice in a row (when there are 2+ entries).")]
        [SerializeField] private bool avoidRepeats = true;

        [Tooltip("Apply the first entry immediately on start instead of transitioning into it.")]
        [SerializeField] private bool applyFirstImmediately = true;

        private int   _index = -1;
        private float _timer;
        private bool  _running;

        /// <summary>True while the cycle is running.</summary>
        public bool IsRunning => _running;

        /// <summary>Seconds left until the next automatic change.</summary>
        public float TimeUntilNextChange => Mathf.Max(0f, _timer);

        /// <summary>The entry list. You can modify it at runtime.</summary>
        public List<Entry> Entries => entries;

        public CycleMode Mode
        {
            get => mode;
            set => mode = value;
        }

        private void Awake()
        {
            if (weatherManager == null)
                weatherManager = GetComponentInParent<WeatherManager>();
            if (weatherManager == null)
                weatherManager = FindAnyObjectByType<WeatherManager>();
        }

        private void Start()
        {
            if (playOnStart) Play();
        }

        private void Update()
        {
            if (!_running) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                Advance(transitionDuration);
        }

        // --- Public API ---

        /// <summary>Starts (or restarts) the cycle from the first state.</summary>
        public void Play()
        {
            if (!HasValidEntries())
            {
                Debug.LogWarning("[WeatherCycle] No valid entries. Add at least one weather state.", this);
                return;
            }

            if (weatherManager == null)
            {
                Debug.LogWarning("[WeatherCycle] No WeatherManager found in the scene.", this);
                return;
            }

            _running = true;
            _index   = -1;
            Advance(applyFirstImmediately ? 0f : transitionDuration);
        }

        /// <summary>Pauses automatic changes. The current weather stays as it is.</summary>
        public void Pause() => _running = false;

        /// <summary>Resumes automatic changes after Pause().</summary>
        public void Resume()
        {
            if (_index < 0) { Play(); return; }
            _running = true;
        }

        /// <summary>Immediately moves to the next state (and resets the timer).</summary>
        public void Next() => Advance(transitionDuration);

        // --- Internal ---

        private void Advance(float duration)
        {
            if (weatherManager == null || !HasValidEntries()) { _running = false; return; }

            _index = PickNextIndex();
            var entry = entries[_index];

            weatherManager.SetWeather(entry.state, duration);

            float min = Mathf.Min(entry.minDuration, entry.maxDuration);
            float max = Mathf.Max(entry.minDuration, entry.maxDuration);
            // The hold time starts after the transition has finished.
            _timer = duration + UnityEngine.Random.Range(min, max);
        }

        private int PickNextIndex()
        {
            int count = entries.Count;

            if (mode == CycleMode.Sequential)
            {
                for (int step = 1; step <= count; step++)
                {
                    int i = (_index + step + count) % count;
                    if (IsValid(entries[i])) return i;
                }
                return Mathf.Max(0, _index);
            }

            // Weighted random
            float total = 0f;
            int validCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (!IsValid(entries[i])) continue;
                validCount++;
                if (avoidRepeats && i == _index) continue;
                total += entries[i].weight;
            }

            if (total <= 0f)
            {
                // All weights are zero (or only the current one is valid): fall back.
                for (int i = 0; i < count; i++)
                    if (IsValid(entries[i]) && (!avoidRepeats || i != _index || validCount == 1)) return i;
                return Mathf.Max(0, _index);
            }

            float roll = UnityEngine.Random.value * total;
            int lastCandidate = Mathf.Max(0, _index);
            for (int i = 0; i < count; i++)
            {
                if (!IsValid(entries[i]) || (avoidRepeats && i == _index)) continue;
                lastCandidate = i;
                roll -= entries[i].weight;
                if (roll <= 0f && entries[i].weight > 0f) return i;
            }
            return lastCandidate;   // guards against floating-point rounding
        }

        private bool HasValidEntries()
        {
            if (entries == null) return false;
            foreach (var e in entries)
                if (IsValid(e)) return true;
            return false;
        }

        private static bool IsValid(Entry e) => e != null && e.state != null;
    }
}
