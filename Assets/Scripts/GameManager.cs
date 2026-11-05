using UnityEngine;
using UnityEngine.SceneManagement;

public enum FruitGameMode { Classic, Arcade, Zen }

public class GameManager : Singleton<GameManager>
{
    public TMPro.TextMeshProUGUI scoreText;

    [SerializeField] Blade blade;
    [SerializeField] Spawner spawner;
    [SerializeField] GameObject GameOverBackground;
    [SerializeField] GameObject OpenPauseAndResume;
    [SerializeField] GameObject OpenPanel;
    [SerializeField] float arcadeDuration = 60f;
    [SerializeField] Transform gameplayCamera;

    FruitNinjaUI ui;
    float timeRemaining;
    float lastSliceTime = -10f;
    float runStartTime;
    int lastHudSecond = -1;
    int score;
    int combo;
    int lives;
    bool isGameRunning;
    bool isGameOver;
    bool isPaused;
    int stage = 1;
    int sequenceStep = -1;
    int sequenceProgress;
    int bossHealth;
    float freezeRemaining;
    float eventRemaining;
    float fruitRushRemaining;
    string eventMessage = string.Empty;
    bool bossActive;
    Renderer stageBackground;
    MaterialPropertyBlock stageBlock;
    Vector3 cameraRestPosition;
    float cameraShakeRemaining;
    float cameraShakeStrength;

    public FruitGameMode Mode { get; private set; }
    public int Score => score;
    public int Combo => combo;
    public int Lives => lives;
    public int Stage => stage;
    public int StageGoal => stage * 100;
    public int SequenceStep => sequenceStep;
    public int SequenceProgress => sequenceProgress;
    public bool SequenceActive => sequenceStep >= 0;
    public bool BossActive => bossActive;
    public int BossHealth => bossHealth;
    public string EventMessage => eventMessage;
    public float TimeRemaining => timeRemaining;
    public bool IsGameRunning => isGameRunning;
    public bool IsPaused => isPaused;
    public int ElapsedSeconds => Mathf.FloorToInt(Time.time - runStartTime);
    public bool CanSpawnBomb => Mode != FruitGameMode.Zen;
    public float SpawnIntervalMultiplier => Mathf.Clamp(1f - score * 0.0015f, 0.45f, 1f);
    public float MissLineY { get; private set; }
    public int BestScore => PlayerPrefs.GetInt("FruitNinja_Best_" + Mode, 0);

    protected override void Awake()
    {
        base.Awake();
        ui = FindAnyObjectByType<FruitNinjaUI>();
        if (blade == null) blade = FindAnyObjectByType<Blade>();
        if (spawner == null) spawner = FindAnyObjectByType<Spawner>();
        if (gameplayCamera == null && Camera.main != null) gameplayCamera = Camera.main.transform;
        if (gameplayCamera != null) cameraRestPosition = gameplayCamera.localPosition;
        GameObject background = GameObject.Find("BackGround");
        if (background != null) stageBackground = background.GetComponent<Renderer>();
        stageBlock = new MaterialPropertyBlock();
        if (Camera.main != null)
            MissLineY = Camera.main.ScreenToWorldPoint(new Vector3(0f, 0f, 0f)).y - 2f;
    }

    void LateUpdate()
    {
        if (gameplayCamera == null || cameraShakeRemaining <= 0f) return;
        cameraShakeRemaining -= Time.unscaledDeltaTime;
        if (cameraShakeRemaining <= 0f)
        {
            gameplayCamera.localPosition = cameraRestPosition;
            cameraShakeStrength = 0f;
            return;
        }
        float fade = cameraShakeRemaining / 0.14f;
        float x = Mathf.PerlinNoise(Time.unscaledTime * 32f, 0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0f, Time.unscaledTime * 32f) * 2f - 1f;
        gameplayCamera.localPosition = cameraRestPosition + new Vector3(x, y, 0f) * cameraShakeStrength * fade;
    }

    void ShakeCamera(float strength)
    {
        if (gameplayCamera == null) return;
        cameraShakeRemaining = 0.14f;
        cameraShakeStrength = Mathf.Max(cameraShakeStrength, strength);
    }

    protected override void Start()
    {
        base.Start();
        Time.timeScale = 1f;
        if (blade != null) blade.enabled = false;
        if (spawner != null) spawner.enabled = false;
        if (GameOverBackground != null) GameOverBackground.SetActive(false);
        if (OpenPauseAndResume != null) OpenPauseAndResume.SetActive(false);
        if (OpenPanel != null) OpenPanel.SetActive(false);
        if (ui != null) ui.ShowMainMenu();
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayMenuMusic();
        SetStageTheme(1);
    }

    void Update()
    {
        if (!isGameRunning || isPaused) return;
        if (freezeRemaining > 0f)
        {
            freezeRemaining -= Time.unscaledDeltaTime;
            if (freezeRemaining <= 0f) Time.timeScale = 1f;
        }
        if (eventRemaining > 0f)
        {
            eventRemaining -= Time.unscaledDeltaTime;
            if (eventRemaining <= 0f)
            {
                eventMessage = string.Empty;
                if (ui != null) ui.RefreshHud();
            }
        }
        if (fruitRushRemaining > 0f) fruitRushRemaining -= Time.deltaTime;
        if (Mode == FruitGameMode.Arcade)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                GameOver();
                return;
            }
        }
        int displayedSecond = Mode == FruitGameMode.Arcade ? Mathf.CeilToInt(timeRemaining) : ElapsedSeconds;
        if (displayedSecond != lastHudSecond)
        {
            lastHudSecond = displayedSecond;
            if (ui != null) ui.RefreshHud();
        }
    }

    public void BeginGame(FruitGameMode mode)
    {
        Time.timeScale = 1f;
        Mode = mode;
        score = 0;
        combo = 0;
        stage = 1;
        sequenceStep = -1;
        sequenceProgress = 0;
        bossHealth = 0;
        bossActive = false;
        freezeRemaining = 0f;
        eventRemaining = 0f;
        fruitRushRemaining = 0f;
        eventMessage = string.Empty;
        lives = 3;
        timeRemaining = mode == FruitGameMode.Arcade ? arcadeDuration : 0f;
        runStartTime = Time.time;
        lastHudSecond = -1;
        lastSliceTime = -10f;
        isGameRunning = true;
        isGameOver = false;
        isPaused = false;
        if (GameOverBackground != null) GameOverBackground.SetActive(false);
        if (OpenPauseAndResume != null) OpenPauseAndResume.SetActive(false);
        if (OpenPanel != null) OpenPanel.SetActive(false);
        if (blade != null) blade.enabled = true;
        if (spawner != null)
        {
            spawner.ResetRun();
            spawner.enabled = true;
        }
        SetStageTheme(1);
        UpdateScoreText();
        if (ui != null)
        {
            ui.ShowGameplay();
            ui.RefreshHud();
        }
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayGameplayMusic();
    }

    public void IncreaseScore(int amount)
    {
        if (!isGameRunning || isGameOver) return;
        combo = Time.time - lastSliceTime <= 0.8f ? combo + 1 : 1;
        lastSliceTime = Time.time;
        score += amount * Mathf.Min(combo, 5) * (fruitRushRemaining > 0f ? 2 : 1);
        if (combo >= 3 && combo % 3 == 0 && FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayCombo();
        if (combo >= 3 && combo % 3 == 0) ShakeCamera(Mathf.Min(0.045f, 0.012f * combo));
        int bestStreak = PlayerPrefs.GetInt("FruitNinja_BestCombo_" + Mode, 0);
        if (combo > bestStreak) PlayerPrefs.SetInt("FruitNinja_BestCombo_" + Mode, combo);
        if (combo >= 10 && PlayerPrefs.GetInt("FruitNinja_Achievement_Combo10", 0) == 0)
        {
            PlayerPrefs.SetInt("FruitNinja_Achievement_Combo10", 1);
            if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayAchievement();
        }
        if (Mode != FruitGameMode.Zen && combo % 10 == 0 && lives < 5)
        {
            lives++;
            AnnounceEvent("COMBO REWARD  +1 LIFE", 2.5f);
        }
        AdvanceProgression();
        UpdateScoreText();
        if (ui != null) ui.RefreshHud();
    }

    void AdvanceProgression()
    {
        int nextStage = 1 + score / 100;
        if (nextStage <= stage) return;
        int previousStage = stage;
        stage = nextStage;
        PlayerPrefs.SetInt("FruitNinja_UnlockedStage", Mathf.Max(stage, PlayerPrefs.GetInt("FruitNinja_UnlockedStage", 1)));
        if (spawner != null) spawner.SetStage(stage);
        SetStageTheme(stage);
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayLevelUp();
        if (previousStage < 2 && stage >= 2 && stage < 3) StartSequenceChallenge();
        if (stage >= 3 && previousStage / 3 < stage / 3) StartBossChallenge();
        AnnounceEvent("STAGE " + stage, 2.5f);
    }

    void SetStageTheme(int value)
    {
        if (stageBackground == null) return;
        if (stageBlock == null) stageBlock = new MaterialPropertyBlock();
        Color[] themes = { Color.white, new Color(1f, 0.92f, 0.78f), new Color(0.82f, 0.92f, 1f), new Color(1f, 0.82f, 0.72f) };
        stageBackground.GetPropertyBlock(stageBlock);
        Color tint = themes[(value - 1) % themes.Length];
        stageBlock.SetColor("_BaseColor", tint);
        stageBlock.SetColor("_Color", tint);
        stageBackground.SetPropertyBlock(stageBlock);
    }

    public void StartSequenceChallenge()
    {
        sequenceStep = 0;
        sequenceProgress = 0;
        if (spawner != null) spawner.BeginSequenceChallenge();
        AnnounceEvent("ORDER: APPLE → MELON → PEAR", 8f);
    }

    public void RegisterSequenceSlice(int step)
    {
        if (!SequenceActive) return;
        if (step != sequenceStep)
        {
            FailSequenceChallenge();
            return;
        }
        sequenceProgress++;
        sequenceStep++;
        score += 10;
        if (sequenceProgress >= 3)
        {
            sequenceStep = -1;
            if (spawner != null) spawner.EndSequenceChallenge();
            score += 30;
            AnnounceEvent("ORDER COMPLETE  +60", 3f);
            AdvanceProgression();
        }
        else
        {
            if (spawner != null) spawner.SequenceStepReady();
            AnnounceEvent("ORDER " + sequenceProgress + "/3", 1.5f);
        }
        UpdateScoreText();
        if (ui != null) ui.RefreshHud();
    }

    public void FailSequenceChallenge()
    {
        if (!SequenceActive) return;
        sequenceStep = -1;
        if (spawner != null) spawner.EndSequenceChallenge();
        AnnounceEvent("ORDER MISSED", 2f);
    }

    public void StartBossChallenge()
    {
        if (bossActive || spawner == null) return;
        if (SequenceActive) FailSequenceChallenge();
        bossActive = true;
        bossHealth = 5;
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayBossMusic();
        spawner.SpawnBossFruit();
        AnnounceEvent("BOSS FRUIT  •  5 HITS", 4f);
    }

    public void RegisterBossHit()
    {
        if (!bossActive) return;
        bossHealth = Mathf.Max(0, bossHealth - 1);
        ShakeCamera(0.018f);
        if (ui != null) ui.RefreshHud();
    }

    public void BossDefeated()
    {
        if (!bossActive) return;
        ShakeCamera(0.06f);
        bossActive = false;
        score += 50;
        PlayerPrefs.SetInt("FruitNinja_Achievement_Boss", 1);
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayAchievement();
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayGameplayMusic();
        AdvanceProgression();
        AnnounceEvent("BOSS DEFEATED  +50", 4f);
        UpdateScoreText();
        if (ui != null) ui.RefreshHud();
    }

    public void BossEscaped()
    {
        if (!bossActive) return;
        bossActive = false;
        bossHealth = 0;
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayGameplayMusic();
        AnnounceEvent("BOSS ESCAPED", 2.5f);
    }

    public void OnSpecialFruit(FruitSpecialType type)
    {
        if (type == FruitSpecialType.Normal || type == FruitSpecialType.Boss) return;
        int count = PlayerPrefs.GetInt("FruitNinja_SpecialFruitCount", 0) + 1;
        PlayerPrefs.SetInt("FruitNinja_SpecialFruitCount", count);
        if (count >= 5) PlayerPrefs.SetInt("FruitNinja_Achievement_Specialist", 1);
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayPowerUp();
    }

    public void HitFakeFruit()
    {
        combo = 0;
        if (Mode != FruitGameMode.Zen) LoseLife();
        if (ui != null) ui.RefreshHud();
        AnnounceEvent("FAKE FRUIT!", 1.5f);
    }

    public void ApplyFreeze(float duration)
    {
        freezeRemaining = Mathf.Max(freezeRemaining, duration);
        if (!isPaused) Time.timeScale = 0.55f;
        AnnounceEvent("TIME FROZEN", duration);
    }

    public void StartFruitRush(float duration)
    {
        fruitRushRemaining = Mathf.Max(fruitRushRemaining, duration);
        AnnounceEvent("FRUIT RUSH  •  DOUBLE SCORE", duration);
    }

    public void AnnounceEvent(string message, float duration = 3f)
    {
        eventMessage = message;
        eventRemaining = duration;
        if (ui != null) ui.RefreshHud();
    }

    public void DecreaseScore(int amount = 5)
    {
        if (!isGameRunning || isGameOver) return;
        score = Mathf.Max(0, score - amount);
        UpdateScoreText();
        if (score == 0) GameOver();
    }

    public void Explode()
    {
        combo = 0;
        ShakeCamera(0.12f);
        if (Mode != FruitGameMode.Zen) LoseLife();
        else if (ui != null) ui.RefreshHud();
    }

    public void MissedFruit()
    {
        if (!isGameRunning || isGameOver) return;
        combo = 0;
        if (SequenceActive) FailSequenceChallenge();
        if (Mode != FruitGameMode.Zen) LoseLife();
        else if (ui != null) ui.RefreshHud();
    }

    void LoseLife()
    {
        lives = Mathf.Max(0, lives - 1);
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayLifeLoss();
        if (lives == 0) GameOver();
        else if (ui != null) ui.RefreshHud();
    }

    void UpdateScoreText()
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        isGameRunning = false;
        isPaused = false;
        Time.timeScale = 1f;
        if (blade != null) blade.enabled = false;
        if (spawner != null) spawner.enabled = false;
        if (score > BestScore)
        {
            PlayerPrefs.SetInt("FruitNinja_Best_" + Mode, score);
            PlayerPrefs.Save();
        }
        PlayerPrefs.SetInt("FruitNinja_UnlockedStage", Mathf.Max(stage, PlayerPrefs.GetInt("FruitNinja_UnlockedStage", 1)));
        PlayerPrefs.Save();
        if (GameOverBackground != null) GameOverBackground.SetActive(false);
        if (OpenPauseAndResume != null) OpenPauseAndResume.SetActive(false);
        if (ui != null) ui.ShowGameOver();
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayGameOverMusic();
    }

    public void RestartRun() => BeginGame(Mode);

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        isGameRunning = false;
        isGameOver = false;
        isPaused = false;
        freezeRemaining = 0f;
        sequenceStep = -1;
        bossActive = false;
        eventMessage = string.Empty;
        eventRemaining = 0f;
        fruitRushRemaining = 0f;
        if (blade != null) blade.enabled = false;
        if (spawner != null) spawner.enabled = false;
        if (ui != null) ui.ShowMainMenu();
        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayMenuMusic();
        SetStageTheme(1);
    }

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void ExitGame() => Application.Quit();
    public void OpenPauseandResume() => PauseGame();
    public void TogglePause() { if (isPaused) ResumeGame(); else PauseGame(); }

    public void PauseGame()
    {
        if (!isGameRunning || isPaused) return;
        Time.timeScale = 0f;
        isPaused = true;
        if (ui != null) ui.ShowPause();
    }

    public void ResumeGame()
    {
        if (!isGameRunning || !isPaused) return;
        Time.timeScale = freezeRemaining > 0f ? 0.55f : 1f;
        isPaused = false;
        if (OpenPanel != null) OpenPanel.SetActive(false);
        if (ui != null) ui.ShowGameplay();
    }
}
