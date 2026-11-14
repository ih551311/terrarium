using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Central orchestrator for the Dynamic Weather System.
    ///
    /// Basic usage:
    ///   - Use GameObject > Dynamic Weather System > Weather System to create a ready-made setup,
    ///     or add this component to a root GameObject and add modules as children.
    ///   - Assign an initial state in the Inspector.
    ///   - Call SetWeather(state) to change the weather using the default transition duration.
    ///   - Call SetWeather(state, duration) to specify a custom duration in seconds.
    ///   - Call SetWeather(state, 0f) for an immediate change with no transition.
    ///
    /// All modules (LightModule, FogModule, etc.) must be children of this GameObject.
    /// They are discovered automatically on Awake. Call RefreshModules() if you add
    /// modules at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dynamic Weather System/Weather Manager")]
    public class WeatherManager : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Weather state applied immediately when the scene starts. Can be left empty.")]
        [SerializeField] private WeatherStateData initialState;

        [Header("Transition")]
        [Tooltip("Duration in seconds used by SetWeather(state) when no duration is specified.")]
        [SerializeField, Min(0f)] private float defaultTransitionDuration = 2f;

        [Tooltip("Curve that controls transition easing. " +
                 "EaseInOut produces the most natural results.")]
        [SerializeField] private AnimationCurve transitionCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Events")]
        [Tooltip("Invoked when a weather change starts. Receives the target state.")]
        [SerializeField] private UnityEvent<WeatherStateData> onWeatherChangeStarted =
            new UnityEvent<WeatherStateData>();

        [Tooltip("Invoked when the target state has been fully reached. Receives the new state.")]
        [SerializeField] private UnityEvent<WeatherStateData> onWeatherChanged =
            new UnityEvent<WeatherStateData>();

        [Header("Debug")]
        [Tooltip("Logs transition start/end messages to the Console.")]
        [SerializeField] private bool logTransitions = false;

        // --- Internal State ---
        private WeatherModule[] _modules = Array.Empty<WeatherModule>();
        private WeatherStateData _currentState;
        private WeatherStateData _fromState;
        private WeatherStateData _runtimeSnapshot;
        private Coroutine _activeTransition;
        private float _transitionProgress;

        // --- Public API ---

        /// <summary>
        /// Raised when a weather change starts. Arguments: (previous state, target state).
        /// The previous state can be null on the first change.
        /// </summary>
        public event Action<WeatherStateData, WeatherStateData> WeatherChangeStarted;

        /// <summary>Raised when the target state has been fully reached.</summary>
        public event Action<WeatherStateData> WeatherChanged;

        /// <summary>The target state of the last SetWeather call.</summary>
        public WeatherStateData CurrentState => _currentState;

        /// <summary>True while a transition is running.</summary>
        public bool IsTransitioning => _activeTransition != null;

        /// <summary>
        /// Current transition progress [0, 1], already evaluated through the AnimationCurve.
        /// Returns 0 when no transition is active.
        /// </summary>
        public float TransitionProgress => _transitionProgress;

        /// <summary>Default transition duration in seconds.</summary>
        public float DefaultTransitionDuration
        {
            get => defaultTransitionDuration;
            set => defaultTransitionDuration = Mathf.Max(0f, value);
        }

        /// <summary>The modules currently driven by this manager.</summary>
        public IReadOnlyList<WeatherModule> Modules => _modules;

        // --- Lifecycle ---

        private void Awake()
        {
            RefreshModules();

            if (_modules.Length == 0)
                Debug.LogWarning("[WeatherManager] No child modules found. " +
                    "Add LightModule, FogModule, or other modules as child GameObjects.", this);
        }

        private void Start()
        {
            if (initialState != null)
                SetWeather(initialState, 0f);
        }

        private void OnDestroy()
        {
            DestroySnapshot();
        }

        // --- Public Methods ---

        /// <summary>
        /// Re-scans child GameObjects for modules. Call this after adding or removing
        /// modules at runtime. The current state is re-applied to the new set of modules.
        /// </summary>
        public void RefreshModules()
        {
            _modules = GetComponentsInChildren<WeatherModule>(includeInactive: true);

            if (_currentState != null && _activeTransition == null)
                ApplyToAll(_currentState);
        }

        /// <summary>Returns the first module of type T, or null if none is present.</summary>
        public T GetModule<T>() where T : WeatherModule
        {
            foreach (var module in _modules)
                if (module is T typed) return typed;
            return null;
        }

        /// <summary>
        /// Transitions to the given state using the default transition duration.
        /// </summary>
        public void SetWeather(WeatherStateData state)
            => SetWeather(state, defaultTransitionDuration);

        /// <summary>
        /// Transitions to the given state with a specific duration in seconds.
        /// If duration is 0 or less, the change is immediate.
        /// If called during an active transition, the current transition is interrupted
        /// and the new one begins from the exact current visual state.
        /// </summary>
        public void SetWeather(WeatherStateData state, float duration)
        {
            if (state == null)
            {
                Debug.LogWarning("[WeatherManager] The provided state is null. " +
                    "Make sure to assign a valid WeatherStateData.", this);
                return;
            }

            var previous = _currentState;

            // If a transition is active, capture the current visual state
            // as the starting point for the new transition.
            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
                _activeTransition = null;
                _fromState = CreateSnapshot(_fromState, _currentState, _transitionProgress);
            }
            else
            {
                _fromState = _currentState;
            }

            _currentState = state;
            _transitionProgress = 0f;

            NotifyStarted(previous, state);

            if (duration <= 0f || _fromState == null)
            {
                ApplyToAll(state);
                DestroySnapshot();
                NotifyCompleted(state);
                return;
            }

            _activeTransition = StartCoroutine(TransitionRoutine(_fromState, state, duration));
        }

        // --- Internal Logic ---

        private IEnumerator TransitionRoutine(WeatherStateData from, WeatherStateData to, float duration)
        {
            if (logTransitions)
                Debug.Log($"[WeatherManager] Starting transition: " +
                          $"<b>{from.stateName}</b> → <b>{to.stateName}</b> ({duration:F1}s)", this);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rawT = Mathf.Clamp01(elapsed / duration);
                _transitionProgress = transitionCurve.Evaluate(rawT);

                BlendAll(from, to, _transitionProgress);
                yield return null;
            }

            // Apply the clean final state to avoid floating-point drift
            _transitionProgress = 1f;
            ApplyToAll(to);
            _activeTransition = null;

            // The snapshot is no longer needed
            DestroySnapshot();

            NotifyCompleted(to);
        }

        private void NotifyStarted(WeatherStateData previous, WeatherStateData target)
        {
            WeatherChangeStarted?.Invoke(previous, target);
            onWeatherChangeStarted?.Invoke(target);
        }

        private void NotifyCompleted(WeatherStateData state)
        {
            if (logTransitions)
                Debug.Log($"[WeatherManager] Weather is now: <b>{state.stateName}</b>", this);

            WeatherChanged?.Invoke(state);
            onWeatherChanged?.Invoke(state);
        }

        private void ApplyToAll(WeatherStateData state)
        {
            foreach (var module in _modules)
            {
                if (module == null) continue;
                module.Apply(state);
            }
        }

        private void BlendAll(WeatherStateData from, WeatherStateData to, float t)
        {
            foreach (var module in _modules)
            {
                if (module == null) continue;
                module.Blend(from, to, t);
            }
        }

        /// <summary>
        /// Creates a temporary ScriptableObject representing the interpolated visual state
        /// at a given transition progress. Used only when a transition is interrupted by another.
        /// </summary>
        private WeatherStateData CreateSnapshot(WeatherStateData from, WeatherStateData to, float t)
        {
            if (from == null) return to;

            // Build the new snapshot first: 'from' may itself be the previous snapshot.
            var snapshot = ScriptableObject.CreateInstance<WeatherStateData>();
            snapshot.name      = "_Snapshot";
            snapshot.stateName = "_Snapshot";
            snapshot.hideFlags = HideFlags.DontSave;
            WeatherStateData.LerpInto(snapshot, from, to, t);

            DestroySnapshot();
            _runtimeSnapshot = snapshot;
            return _runtimeSnapshot;
        }

        private void DestroySnapshot()
        {
            if (_runtimeSnapshot != null)
            {
                Destroy(_runtimeSnapshot);
                _runtimeSnapshot = null;
            }
        }
    }
}
