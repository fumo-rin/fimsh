using RinCore;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using UnityEditor;
using System.Threading.Tasks;
using System.Linq;

namespace RinCore
{
    #region Funny Explosion
    public partial class GeneralManager
    {
        [SerializeField] GameObject funnyExplosion;
        [SerializeField] ParticleSystem Explosion3D;
        [SerializeField] ACWrapper funnyExplosionSound;
        public static void FunnyExplosion(Vector2 position, float scale = 1f)
        {
            GameObject x = Instantiate(Instance.funnyExplosion, position, Quaternion.identity);
            Destroy(x, 1.02f);
            x.transform.localScale *= scale;
            x.transform.localScale = new(x.transform.localScale.x.Max(0.25f), x.transform.localScale.y.Max(0.25f), 1);
            Instance.funnyExplosionSound.Play(position);
        }
        public struct explosionPacket
        {
            public Vector3 position;
            public bool is3d;
            public float scale;
            public bool playSound;
            public explosionPacket(Vector3 position, bool is3d = false)
            {
                this.position = position;
                this.is3d = is3d;
                this.scale = 1f;
                this.playSound = true;
            }
        }
        public static void FunnyExplosion(explosionPacket packet)
        {
            if (!packet.is3d)
            {
                FunnyExplosion((Vector2)packet.position, packet.scale);
            }
            else
            {
                if (Instance is GeneralManager g && g.gameObject != null && g.gameObject.activeInHierarchy)
                {
                    g.Explosion3D.EmitSingleCached(packet.position, null, 0f, null, packet.scale);
                    if (packet.playSound) g.funnyExplosionSound.Play(packet.position);
                }
            }
        }
    }
    #endregion
    #region Pause
    public partial class GeneralManager
    {
        public delegate void PauseToggle(bool state);
        public static event PauseToggle WhenPauseToggle;
        public delegate bool FreezePauseAbility();
        public static event FreezePauseAbility BlockTogglePause;
        public static bool IsPaused { get; private set; }
        public static void SetPause(bool state)
        {
            IsPaused = state;
            if (state)
            {
                IsPaused = true;
            }
            else
            {
                IsPaused = false;
            }
            WhenPauseToggle?.Invoke(state);
        }
        [QFSW.QC.Command("-Pause")]
        public static void PauseGame()
        {
            SetPause(true);
        }
        [QFSW.QC.Command("-Unpause")]
        public static void UnPauseGame()
        {
            SetPause(false);
        }
        public static void TogglePause()
        {
            if (BlockTogglePause?.Invoke() == true)
                return;
            SetPause(!IsPaused);
        }
        [QFSW.QC.Command("-timescale")]
        public static void Command_SetTimescale(float timescale)
        {
            TimeSlowHandler.SetTimescale(timescale);
            UnPauseGame();
        }
        private void PressPauseInput(InputAction.CallbackContext c)
        {
            switch (c.phase)
            {
                case InputActionPhase.Disabled:
                    break;
                case InputActionPhase.Waiting:
                    break;
                case InputActionPhase.Started:
                    break;
                case InputActionPhase.Performed:
                    TogglePause();
                    break;
                case InputActionPhase.Canceled:
                    break;
                default:
                    break;
            }
        }
    }
    #endregion
    #region Score
    public partial class GeneralManager
    {
        private static double HiddenScoreValidationSum = 0;
        private static float ScoreValidationMultiplier;
        public static bool ShouldAddScoreKey => ScoreBreakdownAnalysis != null;
        public static void AddScoreAnalysisKey(string scoreKey, double score)
        {
            if (!ShouldAddScoreKey)
                return;
            if (!ScoreBreakdownAnalysis.ContainsKey(scoreKey))
                ScoreBreakdownAnalysis[scoreKey] = 0;
            ScoreBreakdownAnalysis[scoreKey] += score;
        }
        public delegate bool ScoreValidation();
        public static event ScoreValidation WhenValidateScore;
        public static bool IsScoreLegit()
        {
            bool InternalCheck()
            {
                double scoreAccuracy = HiddenScoreValidationSum / ScoreValidationMultiplier;
                scoreAccuracy = double.IsNaN(scoreAccuracy) ? 0d : scoreAccuracy;
                double totalExpected = actualScore + addedExtraScore;

                if (Math.Abs(scoreAccuracy - totalExpected) < Math.Max(1.0, totalExpected * 0.05))
                {
                    Debug.Log("Score Accuracy is correctish");
                    return true;
                }
                if (actualScore > 0f)
                {
                    Debug.LogWarning("Score Inaccuracy.");
                }
                return false;
            }
            bool ExternalCheck()
            {
                if (WhenValidateScore != null)
                {
                    foreach (ScoreValidation validator in WhenValidateScore.GetInvocationList())
                    {
                        if (!validator.Invoke())
                        {
                            Debug.Log("Score invalidated by external component : " + validator.GetType().Name);
                            return false;
                        }
                    }
                }
                return true;
            }
            bool scoreSubmitable = InternalCheck() && ExternalCheck();
            Debug.Log("Submitable score: " + scoreSubmitable);
            return scoreSubmitable;
        }
        public static double SumUpScoreAnalysis(bool debugPrint)
        {
            double sum = 0f;
            if (ScoreBreakdownAnalysis == null)
            {
                return sum;
            }
            foreach (var item in ScoreBreakdownAnalysis)
            {
                if (debugPrint)
                {
                    Debug.Log($"Score Breakdown({item.Key}) : {item.Value.ToString("F0")}");
                }
                sum += item.Value;
            }
            return sum;
        }
        private static Dictionary<string, double> ScoreBreakdownAnalysis = new();
        [SerializeField] bool breakDownScore = false;
        public static double actualScore;
        public static double addedExtraScore;
        public static double HighestScore { get; private set; }
        public static double VisibleScore => PreProcessedScore + addedExtraScore;
        public static double PreProcessedScore { get; private set; }
        [SerializeField] double visibleScoreDivisor = 0.01d;
        [SerializeField] double visibleScoreMultiplier = 100d;
        static string HighScoreChangeableKey = "";
        static string HighScoreStringKey => BuildHighscoreStringKey(HighScoreChangeableKey);
        static string BuildHighscoreStringKey(string extra)
        {
            return "HiScore Save" + extra;
        }
        static bool IsHighscorePotentiallyOutOfSync = true;
        public delegate void ScoreAction(double score, double hiscore);
        public static ScoreAction OnScoreUpdate;
        public static void ChangeScoreKey(string newKey)
        {
            HighScoreChangeableKey = newKey;
            IsHighscorePotentiallyOutOfSync = true;
            LoadHighScore();
        }
        public static void RequestScoreRefresh()
        {
            IsHighscorePotentiallyOutOfSync = true;
            LoadHighScore();
            AddScore(0d, false);
        }
        public static double LoadHighScore()
        {
            ResyncHighscore();
            return HighestScore;
        }
        public static void StoreAndResetScore()
        {
            long submittedScore = VisibleScore.ToLong();
            bool isLegit = IsScoreLegit();
            SetScoreValue(0f, false);
            addedExtraScore = 0f;
            ScoreBreakdownAnalysis.Clear();
            HiddenScoreValidationSum = 0;
            if (isLegit && !GeneralManager.IsEditor)
            {
                _ = FumoLeaderboard.SubmitScoreAsync(submittedScore);
            }
        }
        [QFSW.QC.Command("-reset-score")]
        private void ResetHighscore(string difficulty)
        {
            PlayerPrefs.DeleteKey(HighScoreStringKey);
            IsHighscorePotentiallyOutOfSync = true;
            ResyncHighscore();
        }
        private static void SendUpdateScoreEvent(double scoreValue, double highScoreValue)
        {
            OnScoreUpdate?.Invoke(scoreValue, highScoreValue);
        }
        public static double AddScore(double value, bool withoutHighscore)
        {
            SetScoreValue(actualScore + value, withoutHighscore);
            HiddenScoreValidationSum += value * ScoreValidationMultiplier;
            return value;
        }
        public static double AddRawExtraScore(double value, bool withoutHighscore)
        {
            addedExtraScore += value;
            HiddenScoreValidationSum += value * ScoreValidationMultiplier;
            SetScoreValue(actualScore, withoutHighscore);
            return value;
        }
        public static void ApplyHighscoreToSave(double value)
        {
            double currentHighscore = 0d.FetchKey(HighScoreStringKey);
            value.Max(currentHighscore).StoreKey(HighScoreStringKey);
            Debug.Log($"Storing Score ({HighScoreStringKey}) : " + value.ToString("F0"));
        }
        public static void ResyncHighscore()
        {
            if (!IsHighscorePotentiallyOutOfSync)
                return;

            IsHighscorePotentiallyOutOfSync = false;

            double loadedScore = 0d.FetchKey(HighScoreStringKey);
            Debug.Log($"Loading Score ({HighScoreStringKey}) : " + loadedScore.ToString("F0"));

            HighestScore = loadedScore;
        }
        private static void SetScoreValue(double value, bool withoutHighscore)
        {
            ResyncHighscore();
            actualScore = value;
            PreProcessedScore = (value.Multiply(Instance.visibleScoreDivisor)).Floor().Multiply(Instance.visibleScoreMultiplier);

            if (!withoutHighscore && actualScore > HighestScore)
            {
                HighestScore = VisibleScore;
            }
            SendUpdateScoreEvent(VisibleScore, HighestScore);
        }
        private void OnApplicationQuit()
        {
            StoreScore(VisibleScore.ToLong());
        }
        public static void StoreScore(long score)
        {
            if (score <= 0)
            {
                return;
            }
            ApplyHighscoreToSave(LoadHighScore());
            if (ScoreBreakdownAnalysis != null)
            {
                foreach (var item in ScoreBreakdownAnalysis)
                {
                    string scoreMessage = "Score Breakdown##".ReplaceLineBreaks("##");
                    scoreMessage += $"Score Partition({item.Key}) : {item.Value.ToString("F0")}##".ReplaceLineBreaks("##");
                }
            }
            if (IsScoreLegit())
            {
                long LeaderboardScore = score;
                _ = FumoLeaderboard.SubmitScoreAsync(LeaderboardScore);
            }
        }
        public static async Task StoreScoreAsyncWait(long score)
        {
            Debug.Log(score);
            if (score <= 0)
            {
                return;
            }
            ApplyHighscoreToSave(LoadHighScore());
            if (ScoreBreakdownAnalysis != null)
            {
                foreach (var item in ScoreBreakdownAnalysis)
                {
                    string scoreMessage = "Score Breakdown##".ReplaceLineBreaks("##");
                    scoreMessage += $"Score Partition({item.Key}) : {item.Value.ToString("F0")}##".ReplaceLineBreaks("##");
                }
            }
            if (IsScoreLegit())
            {
                try
                {
                    long LeaderboardScore = score;
                    await FumoLeaderboard.SubmitScoreAsync(LeaderboardScore);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"StoreScoreAsyncWait failed: {e}");
                }
            }
        }
    }
    #endregion
    #region Score Key Debugger
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class PlaymodeScoreDebugger
    {
        static PlaymodeScoreDebugger()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                GeneralManager.SumUpScoreAnalysis(true);
            }
        }
    }
#endif
    #endregion
    #region Application Determine
    public partial class GeneralManager
    {
        public static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;
        public static bool IsEditor =>
#if UNITY_EDITOR
            true;
#else
            false;
#endif
    }
    #endregion
    [DefaultExecutionOrder(-10)]
    public partial class GeneralManager : MonoBehaviour
    {
        public static GeneralManager Instance { get; private set; }
        [SerializeField] InputActionReference pauseKeybind;
        private void Awake()
        {
            StartInstance();
        }
        [Initialize(-99959595)]
        private static void ClearInstance()
        {
            Instance = null;
        }
        [QFSW.QC.Command("FPS")]
        private static void SetFPS(int fps)
        {
            Application.targetFrameRate = fps.Clamp(5, 120);
        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                CloseInstance();
                if (pauseKeybind != null)
                {
                    pauseKeybind.action.performed -= PressPauseInput;
                    pauseKeybind.action.Disable();
                }
                SceneLoader.WhenStartLoadingAdditives -= PauseGame;
                SceneLoader.WhenFinishedLoadingAdditives -= UnPauseGame;
            }
        }
        private void Start()
        {
            if (Instance == this)
            {
                IsHighscorePotentiallyOutOfSync = true;
                SetScoreValue(0f, false);
                ScoreValidationMultiplier = UnityEngine.Random.Range(1f, 10f);
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                TimeSlowHandler.Reload();
                SceneLoader.WhenStartLoadingAdditives += PauseGame;
                SceneLoader.WhenFinishedLoadingAdditives += UnPauseGame;
                if (pauseKeybind)
                {
                    pauseKeybind.action.Enable();
                    pauseKeybind.action.performed += PressPauseInput;
                }
            }
        }
        private void StartInstance()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SetPause(false);
            DontDestroyOnLoad(gameObject);
            if (breakDownScore)
            {
                ScoreBreakdownAnalysis = new();
            }
            StoreAndResetScore();
        }
        private void CloseInstance()
        {
            if (Instance != this)
                return;
            Instance = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ReInitialize()
        {
            Instance = null;
            ScoreBreakdownAnalysis = null;
        }
    }
}
