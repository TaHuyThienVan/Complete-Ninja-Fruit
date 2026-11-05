using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class FruitNinjaUI : MonoBehaviour
{
    static Sprite roundedButtonSprite;
    static readonly Color Ink = new Color(0.045f, 0.018f, 0.012f, 0.98f);
    static readonly Color Cream = new Color(1f, 0.88f, 0.59f, 1f);
    static readonly Color Green = new Color(0.22f, 0.72f, 0.12f, 1f);
    static readonly Color Orange = new Color(0.98f, 0.35f, 0.06f, 1f);
    static readonly Color Purple = new Color(0.55f, 0.16f, 0.70f, 1f);

    Canvas canvas;
    RectTransform uiRoot;
    GameManager game;
    Blade blade;
    TMP_FontAsset displayFont;
    Texture2D menuBackgroundTexture;
    RawImage menuBackdrop;
    [SerializeField] Texture2D menuFruitTexture;
    [SerializeField] Texture2D menuSplashTexture;
    [SerializeField] Texture2D cardWoodTexture;
    GameObject mainMenu, modesMenu, settingsMenu, highScoreMenu, hud, pauseMenu, gameOverMenu;
    RectTransform[] cards;
    TMP_Text scoreText, comboText, livesText, timerText, stageText, objectiveText, eventText;
    TMP_Text finalScoreText, bestScoreText, highScoreText;
    TMP_Text musicValue, sfxValue, sensitivityValue;
    Slider musicSlider, sfxSlider, sensitivitySlider;
    bool settingsReturnToPause;
    int layoutScreenWidth = -1, layoutScreenHeight = -1;
    Slider masterSlider;
    TMP_Text masterValue, muteLabel;
    int lastDisplayedScore = -1;
    Coroutine scorePulse;
    RectTransform menuFruitArtwork, menuSplashArtwork;
    Vector2 menuFruitPosition, menuSplashPosition;
    float menuFruitRotation, menuSplashRotation;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        game = GameManager.instance;
        blade = FindAnyObjectByType<Blade>();
        displayFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");
        if (displayFont == null) displayFont = TMP_Settings.defaultFontAsset;
        menuBackgroundTexture = Resources.Load<Texture2D>("UI/MainMenuBackground");
        Transform existingRoot = canvas.transform.Find("FruitNinjaUIRoot");
        if (existingRoot == null) BuildUI();
        else BindExistingUI(existingRoot);
        EnsureMenuBackdrop();
        EnsureHudPanels();
        StyleButtonShapes();
        StyleCardWood();
        StyleReferenceScreens();
        EnsureAudioSettings();
        BindSliders();
    }

    void Start()
    {
        HideLegacyPanels();
        LoadSettings();
        if (game != null) ShowMainMenu();
        Canvas.ForceUpdateCanvases();
        layoutScreenWidth = Screen.width;
        layoutScreenHeight = Screen.height;
        ApplyResponsiveLayout(uiRoot.rect.width, (float)Screen.width / Mathf.Max(1, Screen.height));
        CacheMenuArtwork();
    }

    void Update()
    {
        if (Screen.width != layoutScreenWidth || Screen.height != layoutScreenHeight)
        {
            layoutScreenWidth = Screen.width;
            layoutScreenHeight = Screen.height;
            Canvas.ForceUpdateCanvases();
            ApplyResponsiveLayout(uiRoot.rect.width, (float)Screen.width / Mathf.Max(1, Screen.height));
            CacheMenuArtwork();
        }
        AnimateMenuArtwork(menuFruitArtwork, menuFruitPosition, menuFruitRotation, 0f, 7f, 1.1f);
        AnimateMenuArtwork(menuSplashArtwork, menuSplashPosition, menuSplashRotation, 1.4f, 10f, 0.8f);
        if (game != null && game.IsGameRunning && Input.GetKeyDown(KeyCode.Escape))
        {
            if (game.Mode != FruitGameMode.Zen || game.IsPaused) game.TogglePause();
        }
    }

    void CacheMenuArtwork()
    {
        menuFruitArtwork = FindChild(mainMenu.transform, "Menu Watermelon") as RectTransform;
        menuSplashArtwork = FindChild(mainMenu.transform, "Watermelon Juice") as RectTransform;
        if (menuFruitArtwork != null) { menuFruitPosition = menuFruitArtwork.anchoredPosition; menuFruitRotation = menuFruitArtwork.localEulerAngles.z; }
        if (menuSplashArtwork != null) { menuSplashPosition = menuSplashArtwork.anchoredPosition; menuSplashRotation = menuSplashArtwork.localEulerAngles.z; }
    }

    static void AnimateMenuArtwork(RectTransform artwork, Vector2 position, float rotation, float phase, float amplitude, float speed)
    {
        if (artwork == null || !artwork.gameObject.activeInHierarchy) return;
        float wave = Mathf.Sin(Time.unscaledTime * speed + phase);
        artwork.anchoredPosition = position + Vector2.up * (wave * amplitude);
        artwork.localRotation = Quaternion.Euler(0f, 0f, rotation + wave * 2f);
    }

    void ApplyResponsiveLayout(float width, float aspectRatio)
    {
        if (uiRoot == null || cards == null) return;
        bool wide = aspectRatio >= 1.5f;
        float cardWidth = Mathf.Min(780f, width * 0.9f);
        float cardHeight = Mathf.Min(900f, uiRoot.rect.height * 0.9f);
        foreach (RectTransform card in cards)
        {
            if (card == null) continue;
            card.sizeDelta = new Vector2(cardWidth, cardHeight);
        }
        StyleGameModes(cardWidth, cardHeight);

        StyleMainMenuLayout(cardWidth, cardHeight, wide);
        StyleGameOverLayout(cardWidth, wide);

        float fruitWidth = Mathf.Min(330f, width * 0.2f);
        float fruitX = -(cardWidth + fruitWidth) * 0.5f - 24f;
        PlaceMenuArtwork("Watermelon Juice", wide && menuBackdrop == null, fruitX + 12f, -14f, fruitWidth * 1.25f, -7f);
        PlaceMenuArtwork("Menu Watermelon", wide && menuBackdrop == null, fruitX, -18f, fruitWidth, -14f);
        UpdateBackdropCrop(aspectRatio);
    }

    void EnsureMenuBackdrop()
    {
        if (menuBackgroundTexture == null) return;
        Transform existing = FindChild(uiRoot, "MainMenuBackdrop");
        GameObject backdrop = existing != null ? existing.gameObject : new GameObject("MainMenuBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        if (existing == null) backdrop.transform.SetParent(uiRoot, false);
        backdrop.layer = uiRoot.gameObject.layer;
        Stretch(backdrop.GetComponent<RectTransform>());
        menuBackdrop = backdrop.GetComponent<RawImage>();
        menuBackdrop.texture = menuBackgroundTexture;
        menuBackdrop.color = Color.white;
        menuBackdrop.raycastTarget = false;
        backdrop.transform.SetAsFirstSibling();
    }

    void UpdateBackdropCrop(float aspectRatio)
    {
        Texture texture = menuBackdrop != null ? menuBackdrop.texture : null;
        if (texture == null) return;
        float imageAspect = (float)texture.width / texture.height;
        if (aspectRatio > imageAspect)
        {
            float height = imageAspect / aspectRatio;
            menuBackdrop.uvRect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
        else
        {
            float width = aspectRatio / imageAspect;
            float x = aspectRatio < 1f ? 0f : (1f - width) * 0.5f;
            menuBackdrop.uvRect = new Rect(x, 0f, width, 1f);
        }
    }

    void PlaceMenuArtwork(string name, bool visible, float x, float y, float width, float rotation)
    {
        Transform artwork = FindChild(mainMenu.transform, name);
        if (artwork == null) return;
        RawImage image = artwork.GetComponent<RawImage>();
        visible &= image != null && image.texture != null;
        artwork.gameObject.SetActive(visible);
        if (!visible) return;

        RectTransform rect = artwork as RectTransform;
        rect.sizeDelta = new Vector2(width, width * image.texture.height / image.texture.width);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    void BuildUI()
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject root = new GameObject("FruitNinjaUIRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        root.layer = canvas.gameObject.layer;
        uiRoot = root.GetComponent<RectTransform>();
        Stretch(uiRoot);

        mainMenu = CreateScreen("MainMenu", true, out RectTransform mainCard);
        BuildMainMenu(mainCard);
        modesMenu = CreateScreen("GameModes", true, out RectTransform modesCard);
        BuildModes(modesCard);
        settingsMenu = CreateScreen("Settings", true, out RectTransform settingsCard);
        BuildSettings(settingsCard);
        highScoreMenu = CreateScreen("HighScore", true, out RectTransform scoresCard);
        BuildHighScore(scoresCard);
        hud = CreateScreen("GameplayHUD", false, out RectTransform hudCard);
        hudCard.gameObject.SetActive(false);
        BuildHud((RectTransform)hud.transform);
        pauseMenu = CreateScreen("Pause", true, out RectTransform pauseCard);
        BuildPause(pauseCard);
        gameOverMenu = CreateScreen("GameOver", true, out RectTransform overCard);
        BuildGameOver(overCard);
        cards = new[] { mainCard, modesCard, settingsCard, scoresCard, pauseCard, overCard };
        if (Application.isPlaying) SetScreensVisible(null);
        else SetScreensVisible(mainMenu);
    }

    void BindExistingUI(Transform existingRoot)
    {
        uiRoot = existingRoot.GetComponent<RectTransform>();
        mainMenu = FindChild(uiRoot, "MainMenu").gameObject;
        modesMenu = FindChild(uiRoot, "GameModes").gameObject;
        settingsMenu = FindChild(uiRoot, "Settings").gameObject;
        highScoreMenu = FindChild(uiRoot, "HighScore").gameObject;
        hud = FindChild(uiRoot, "GameplayHUD").gameObject;
        pauseMenu = FindChild(uiRoot, "Pause").gameObject;
        gameOverMenu = FindChild(uiRoot, "GameOver").gameObject;
        cards = new[]
        {
            FindChild(mainMenu.transform, "MainMenu Card") as RectTransform,
            FindChild(modesMenu.transform, "GameModes Card") as RectTransform,
            FindChild(settingsMenu.transform, "Settings Card") as RectTransform,
            FindChild(highScoreMenu.transform, "HighScore Card") as RectTransform,
            FindChild(pauseMenu.transform, "Pause Card") as RectTransform,
            FindChild(gameOverMenu.transform, "GameOver Card") as RectTransform
        };
        scoreText = FindChild(hud.transform, "Text SCORE 000000").GetComponent<TMP_Text>();
        timerText = FindChild(hud.transform, "Text TIME ∞").GetComponent<TMP_Text>();
        stageText = FindChild(hud.transform, "Text STAGE 1").GetComponent<TMP_Text>();
        objectiveText = FindChild(hud.transform, "Text GOAL 100").GetComponent<TMP_Text>();
        eventText = FindChild(hud.transform, "Text EVENT").GetComponent<TMP_Text>();
        livesText = FindChild(hud.transform, "Text ♥ ♥ ♥").GetComponent<TMP_Text>();
        comboText = FindChild(hud.transform, "Text ").GetComponent<TMP_Text>();
        finalScoreText = TextOccurrence(FindChild(gameOverMenu.transform, "GameOver Card"), "Text 0", 0);
        bestScoreText = TextOccurrence(FindChild(gameOverMenu.transform, "GameOver Card"), "Text 0", 1);
        highScoreText = FindChild(highScoreMenu.transform, "Text ").GetComponent<TMP_Text>();
        musicSlider = FindChild(settingsMenu.transform, "MUSIC Slider").GetComponent<Slider>();
        sfxSlider = FindChild(settingsMenu.transform, "SFX Slider").GetComponent<Slider>();
        sensitivitySlider = FindChild(settingsMenu.transform, "SENSITIVITY Slider").GetComponent<Slider>();
        musicValue = TextOccurrence(FindChild(settingsMenu.transform, "Settings Card"), "Text 70%", 0);
        sfxValue = TextOccurrence(FindChild(settingsMenu.transform, "Settings Card"), "Text 70%", 1);
        sensitivityValue = TextOccurrence(FindChild(settingsMenu.transform, "Settings Card"), "Text 70%", 2);

        BindButton(mainMenu.transform, "START Button", StartModes);
        BindButton(mainMenu.transform, "GAME MODES Button", StartModes);
        BindButton(mainMenu.transform, "HIGH SCORE Button", ShowHighScore);
        BindButton(mainMenu.transform, "SETTINGS Button", OpenMainSettings);
        BindButton(mainMenu.transform, "QUIT Button", () => game.ExitGame());
        BindButton(modesMenu.transform, "CLASSIC Button", () => game.BeginGame(FruitGameMode.Classic));
        BindButton(modesMenu.transform, "ARCADE Button", () => game.BeginGame(FruitGameMode.Arcade));
        BindButton(modesMenu.transform, "ZEN Button", () => game.BeginGame(FruitGameMode.Zen));
        BindButton(modesMenu.transform, "BACK Button", ShowMainMenu);
        BindButton(settingsMenu.transform, "BACK Button", BackFromSettings);
        BindButton(highScoreMenu.transform, "BACK Button", ShowMainMenu);
        BindButton(hud.transform, "II Button", game.PauseGame);
        BindButton(pauseMenu.transform, "RESUME Button", game.ResumeGame);
        BindButton(pauseMenu.transform, "RESTART Button", game.RestartRun);
        BindButton(pauseMenu.transform, "SETTINGS Button", OpenPauseSettings);
        BindButton(pauseMenu.transform, "MAIN MENU Button", game.ReturnToMenu);
        BindButton(gameOverMenu.transform, "PLAY AGAIN Button", game.RestartRun);
        BindButton(gameOverMenu.transform, "MAIN MENU Button", game.ReturnToMenu);
    }

    void BindSliders()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
        sensitivitySlider.onValueChanged.RemoveAllListeners();
        masterSlider.onValueChanged.AddListener(value => { masterValue.text = Percent(value); if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.SetMasterVolume(value); });
        musicSlider.onValueChanged.AddListener(value => { musicValue.text = Percent(value); if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.SetMusicVolume(value); });
        sfxSlider.onValueChanged.AddListener(value => { sfxValue.text = Percent(value); if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.SetSfxVolume(value); });
        sensitivitySlider.onValueChanged.AddListener(value =>
        {
            sensitivityValue.text = Percent(value);
            if (blade != null) blade.SetSensitivity(value);
            PlayerPrefs.SetFloat("FruitNinja_Sensitivity", value);
        });
    }

    void BindButton(Transform scope, string name, Action action)
    {
        Button button = FindChild(scope, name).GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => { if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayUiClick(); action(); });
        AddHoverSound(button);
    }

    void AddHoverSound(Button button)
    {
        if (button.GetComponent<ButtonMotion>() == null) button.gameObject.AddComponent<ButtonMotion>();
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
        trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerEnter);
        EventTrigger.Entry hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hover.callback.AddListener(_ => { if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayUiHover(); });
        trigger.triggers.Add(hover);
    }

    void EnsureAudioSettings()
    {
        Transform card = FindChild(settingsMenu.transform, "Settings Card");
        if (FindChild(card, "MASTER Slider") == null) masterSlider = SettingsSlider((RectTransform)card, "MASTER", Vector2.zero, out masterValue);
        else
        {
            masterSlider = FindChild(card, "MASTER Slider").GetComponent<Slider>();
            masterValue = TextOccurrence(card, "Text 70%", 3);
        }
        PlaceSettingRow(masterSlider, FindChild(card, "Text MASTER").GetComponent<TMP_Text>(), masterValue, 195f);
        PlaceSettingRow(musicSlider, FindChild(card, "Text MUSIC").GetComponent<TMP_Text>(), musicValue, 75f);
        PlaceSettingRow(sfxSlider, FindChild(card, "Text SFX").GetComponent<TMP_Text>(), sfxValue, -45f);
        PlaceSettingRow(sensitivitySlider, FindChild(card, "Text SENSITIVITY").GetComponent<TMP_Text>(), sensitivityValue, -165f);
        if (FindChild(card, "MUTE Button") == null)
            Button((RectTransform)card, "MUTE", new Color(0.48f, 0.20f, 0.09f), new Vector2(0f, -260f), ToggleMute, 52f);
        Transform mute = FindChild(card, "MUTE Button");
        muteLabel = mute.GetComponentInChildren<TMP_Text>();
        Transform back = FindChild(card, "BACK Button");
        if (back != null) back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -355f);
        RefreshMuteLabel();
    }

    void PlaceSettingRow(Slider slider, TMP_Text label, TMP_Text value, float y)
    {
        slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y - 12f);
        label.rectTransform.anchoredPosition = new Vector2(-245f, y + 35f);
        value.rectTransform.anchoredPosition = new Vector2(244f, y + 35f);
    }

    void ToggleMute()
    {
        if (FruitNinjaAudio.Instance == null) return;
        FruitNinjaAudio.Instance.SetMuted(!FruitNinjaAudio.Instance.Muted);
        RefreshMuteLabel();
    }

    void RefreshMuteLabel()
    {
        if (muteLabel != null) muteLabel.text = FruitNinjaAudio.Instance != null && FruitNinjaAudio.Instance.Muted ? "MUTED" : "MUTE: OFF";
    }

    TMP_Text TextOccurrence(Transform scope, string name, int occurrence)
    {
        TMP_Text[] texts = scope.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
            if (text.gameObject.name == name && occurrence-- == 0) return text;
        return null;
    }

    GameObject CreateScreen(string name, bool dim, out RectTransform card)
    {
        GameObject screen = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        screen.transform.SetParent(uiRoot, false);
        screen.layer = uiRoot.gameObject.layer;
        RectTransform screenRect = screen.GetComponent<RectTransform>();
        Stretch(screenRect);
        if (dim) CreateImage("Dim", screen.transform, new Color(0.035f, 0.012f, 0.02f, 0.66f), true, Vector2.zero, Vector2.zero, true);
        if (name == "MainMenu") CreateMenuArtwork(screen.transform);
        else if (dim && menuSplashTexture != null && 1920f * Screen.width / Mathf.Max(1f, Screen.height) >= 1200f)
            AddMenuTexture(screen.transform, name + " Juice Accent", menuSplashTexture, -470f, -340f, 280f, -8f, 0.72f);

        GameObject cardObject = new GameObject(name + " Card", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardObject.transform.SetParent(screen.transform, false);
        cardObject.layer = screen.layer;
        card = cardObject.GetComponent<RectTransform>();
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        float width = Mathf.Min(780f, 1920f * Screen.width / Mathf.Max(1f, Screen.height) * 0.9f);
        card.sizeDelta = new Vector2(width, 900f);
        float referenceWidth = 1920f * Screen.width / Mathf.Max(1f, Screen.height);
        card.anchoredPosition = name == "MainMenu" && referenceWidth >= 1200f ? new Vector2(referenceWidth * 0.12f, 0f) : Vector2.zero;
        card.GetComponent<Image>().color = Ink;
        Outline outline = cardObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.62f, 0.18f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        if (cardWoodTexture != null)
        {
            GameObject grain = new GameObject("Wood Grain", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            grain.transform.SetParent(card, false);
            grain.layer = card.gameObject.layer;
            Stretch(grain.GetComponent<RectTransform>());
            RawImage woodImage = grain.GetComponent<RawImage>();
            woodImage.texture = cardWoodTexture;
            woodImage.color = new Color(0.30f, 0.12f, 0.045f, 0.20f);
            woodImage.raycastTarget = false;
        }
        return screen;
    }

    void StyleCardWood()
    {
        foreach (RectTransform card in cards)
        {
            if (card == null) continue;
            foreach (RawImage grain in card.GetComponentsInChildren<RawImage>(true))
                if (grain.gameObject.name == "Wood Grain") grain.color = new Color(0.30f, 0.12f, 0.045f, 0.20f);
        }
    }

    void StyleReferenceScreens()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            RectTransform card = cards[i];
            if (card == null) continue;
            Image image = card.GetComponent<Image>();
            Outline outline = card.GetComponent<Outline>();
            bool parchment = i >= 2;
            if (image != null)
            {
                image.raycastTarget = false;
                image.sprite = parchment ? roundedButtonSprite : null;
                image.type = Image.Type.Sliced;
                image.color = parchment ? new Color(1f, 0.80f, 0.48f, 1f) : new Color(1f, 1f, 1f, 0f);
            }
            if (outline != null)
            {
                outline.enabled = parchment;
                outline.effectColor = new Color(0.38f, 0.13f, 0.035f, 0.96f);
                outline.effectDistance = new Vector2(5f, -5f);
            }
            Transform grain = FindChild(card, "Wood Grain");
            if (grain != null) grain.gameObject.SetActive(false);
            if (parchment && card.GetComponent<Shadow>() == null)
            {
                Shadow shadow = card.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.04f, 0.012f, 0.005f, 0.7f);
                shadow.effectDistance = new Vector2(0f, -12f);
            }
        }

        AddTitleRibbon(cards[1], "GAME MODES", new Color(0.42f, 0.17f, 0.045f, 1f));
        AddTitleRibbon(cards[2], "SETTINGS", new Color(0.42f, 0.17f, 0.045f, 1f));
        AddTitleRibbon(cards[3], "HIGH SCORE", new Color(0.42f, 0.17f, 0.045f, 1f));
        AddTitleRibbon(cards[4], "PAUSE", new Color(0.76f, 0.045f, 0.025f, 1f));
        if (cards[4] != null)
            foreach (TMP_Text label in cards[4].GetComponentsInChildren<TMP_Text>(true))
                if (label.text == "PAUSED") { label.text = "PAUSE"; break; }
        AddTitleRibbon(cards[5], "GAME OVER", new Color(0.76f, 0.045f, 0.025f, 1f));
    }

    void AddTitleRibbon(RectTransform card, string title, Color color)
    {
        if (card == null || FindChild(card, "Reference Title Ribbon") != null) return;
        TMP_Text label = null;
        foreach (TMP_Text candidate in card.GetComponentsInChildren<TMP_Text>(true))
            if (candidate.text.Contains(title)) { label = candidate; break; }
        if (label == null) return;

        GameObject ribbon = new GameObject("Reference Title Ribbon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
        ribbon.transform.SetParent(card, false);
        ribbon.transform.SetAsFirstSibling();
        ribbon.layer = card.gameObject.layer;
        RectTransform rect = ribbon.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = label.rectTransform.anchoredPosition;
        rect.sizeDelta = new Vector2(Mathf.Min(590f, label.rectTransform.sizeDelta.x * 0.86f), label.rectTransform.sizeDelta.y * 0.78f);
        Image image = ribbon.GetComponent<Image>();
        image.sprite = roundedButtonSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        Outline outline = ribbon.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.54f, 0.16f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = ribbon.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.04f, 0.01f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -7f);
    }

    void StyleMainMenuLayout(float cardWidth, float cardHeight, bool wide)
    {
        if (cards.Length == 0 || cards[0] == null) return;
        RectTransform card = cards[0];
        card.anchoredPosition = Vector2.zero;
        TMP_Text logo = null;
        foreach (TMP_Text label in mainMenu.GetComponentsInChildren<TMP_Text>(true))
            if (label.text.Contains("FRUIT") && label.text.Contains("NINJA")) { logo = label; break; }
        if (logo != null)
        {
            logo.rectTransform.anchoredPosition = wide ? new Vector2(-cardWidth * 0.29f, 205f) : new Vector2(0f, cardHeight * 0.31f);
            logo.rectTransform.sizeDelta = wide ? new Vector2(cardWidth * 0.56f, 300f) : new Vector2(cardWidth * 0.92f, 230f);
            logo.fontSize = wide ? 104f : 84f;
        }
        Transform tagline = FindChild(mainMenu.transform, "Text SLICE • SURVIVE • REPEAT");
        if (tagline != null) tagline.gameObject.SetActive(false);

        string[] names = { "START Button", "GAME MODES Button", "HIGH SCORE Button", "SETTINGS Button", "QUIT Button" };
        float width = Mathf.Min(wide ? 500f : 560f, cardWidth * (wide ? 0.62f : 0.86f));
        float top = wide ? 190f : 55f;
        float spacing = wide ? 102f : 88f;
        float x = wide ? cardWidth * 0.29f : 0f;
        for (int i = 0; i < names.Length; i++)
        {
            Transform button = FindChild(mainMenu.transform, names[i]);
            if (button == null) continue;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, wide ? 78f : 70f);
            rect.anchoredPosition = new Vector2(x, top - spacing * i);
        }
    }

    void StyleGameOverLayout(float cardWidth, bool wide)
    {
        if (cards.Length <= 5 || cards[5] == null) return;
        string[] names = { "PLAY AGAIN Button", "MAIN MENU Button" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform button = FindChild(cards[5], names[i]);
            if (button == null) continue;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = wide ? new Vector2(Mathf.Min(390f, cardWidth * 0.48f), 74f) : new Vector2(cardWidth * 0.76f, 68f);
            rect.anchoredPosition = wide ? new Vector2(i == 0 ? -205f : 205f, -330f) : new Vector2(0f, i == 0 ? -255f : -335f);
        }
    }

    void StyleButtonShapes()
    {
        if (roundedButtonSprite == null)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Rounded UI Button", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - 10f);
                    float qy = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - 10f);
                    float distance = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - 10f;
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-1.25f, 1.25f, distance));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            roundedButtonSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18f, 18f, 18f, 18f));
        }

        foreach (Button button in uiRoot.GetComponentsInChildren<Button>(true))
        {
            Image image = button.targetGraphic as Image;
            if (image == null) continue;
            image.sprite = roundedButtonSprite;
            image.type = Image.Type.Sliced;
        }
    }

    void StyleGameModes(float cardWidth, float cardHeight)
    {
        string[] names = { "CLASSIC Button", "ARCADE Button", "ZEN Button" };
        float width = Mathf.Min(220f, (cardWidth - 64f) / 3f);
        float spacing = width + 20f;
        float height = Mathf.Min(270f, cardHeight * 0.34f);
        float labelSize = Mathf.Clamp(width * 0.17f, 22f, 38f);
        float detailSize = Mathf.Clamp(width * 0.095f, 15f, 22f);
        for (int i = 0; i < names.Length; i++)
        {
            Transform item = FindChild(modesMenu.transform, names[i]);
            if (item == null) continue;
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2((i - 1) * spacing, 8f);
            Image image = item.GetComponent<Image>();
            Outline outline = item.GetComponent<Outline>();
            if (image != null && outline != null) outline.effectColor = Color.Lerp(image.color, Color.white, 0.45f);
            TMP_Text[] labels = item.GetComponentsInChildren<TMP_Text>(true);
            if (labels.Length > 0)
            {
                labels[0].fontSize = labelSize;
                labels[0].rectTransform.anchoredPosition = new Vector2(0f, height * 0.22f);
                labels[0].rectTransform.sizeDelta = new Vector2(width - 16f, 58f);
            }
            if (labels.Length > 1)
            {
                labels[1].text = i == 0 ? "3 mạng, chém bom\nsẽ thua." : i == 1 ? "Giới hạn 60 giây,\nđiểm thưởng cao!" : "Chơi thư giãn,\nkhông có bom.";
                labels[1].fontSize = detailSize;
                labels[1].rectTransform.anchoredPosition = new Vector2(0f, -height * 0.12f);
                labels[1].rectTransform.sizeDelta = new Vector2(width - 24f, height * 0.42f);
            }
        }
    }

    void CreateMenuArtwork(Transform parent)
    {
        float referenceWidth = 1920f * Screen.width / Mathf.Max(1f, Screen.height);
        if (referenceWidth < 1200f || menuFruitTexture == null) return;
        float cardWidth = Mathf.Min(780f, referenceWidth * 0.9f);
        float fruitWidth = Mathf.Min(330f, referenceWidth * 0.2f);
        float centerX = -(cardWidth + fruitWidth) * 0.5f - 24f;
        if (menuSplashTexture != null)
            AddMenuTexture(parent, "Watermelon Juice", menuSplashTexture, centerX + 12f, -14f, fruitWidth * 1.25f, -7f);
        AddMenuTexture(parent, "Menu Watermelon", menuFruitTexture, centerX, -18f, fruitWidth, -14f);
    }

    void AddMenuTexture(Transform parent, string name, Texture2D texture, float x, float y, float width, float rotation, float alpha = 1f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, width * texture.height / texture.width);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = new Color(1f, 1f, 1f, alpha);
        image.raycastTarget = false;
    }

    void BuildMainMenu(RectTransform card)
    {
        Title(card, "<color=#FFB51B>FRUIT</color>\n<color=#FFF4DF>NINJA</color>", 96, new Vector2(0f, 295f), new Vector2(700f, 230f));
        Text(card, "SLICE • SURVIVE • REPEAT", 24, Cream, new Vector2(0f, 190f), new Vector2(680f, 52f), false);
        Button(card, "START", Green, new Vector2(0f, 86f), StartModes);
        Button(card, "GAME MODES", Purple, new Vector2(0f, -30f), StartModes);
        Button(card, "HIGH SCORE", new Color(0.81f, 0.35f, 0.08f), new Vector2(0f, -146f), ShowHighScore);
        Button(card, "SETTINGS", new Color(0.48f, 0.20f, 0.09f), new Vector2(0f, -262f), OpenMainSettings);
        Button(card, "QUIT", new Color(0.65f, 0.12f, 0.08f), new Vector2(0f, -378f), () => game.ExitGame());
    }

    void BuildModes(RectTransform card)
    {
        Title(card, "GAME MODES", 66, new Vector2(0f, 316f), new Vector2(700f, 120f));
        ModeButton(card, "CLASSIC", "3 lives • fruit and bombs", Green, new Vector2(0f, 154f), FruitGameMode.Classic);
        ModeButton(card, "ARCADE", "60 seconds • rising pace", Orange, new Vector2(0f, 8f), FruitGameMode.Arcade);
        ModeButton(card, "ZEN", "Relaxed slicing • no bombs", Purple, new Vector2(0f, -138f), FruitGameMode.Zen);
        Button(card, "BACK", new Color(0.30f, 0.18f, 0.12f), new Vector2(0f, -304f), ShowMainMenu, 58f);
    }

    void ModeButton(RectTransform parent, string label, string description, Color color, Vector2 position, FruitGameMode mode)
    {
        Button(parent, label, description, color, position, () => game.BeginGame(mode));
    }

    void BuildSettings(RectTransform card)
    {
        Title(card, "SETTINGS", 68, new Vector2(0f, 310f), new Vector2(700f, 120f));
        musicSlider = SettingsSlider(card, "MUSIC", new Vector2(0f, 158f), out musicValue);
        sfxSlider = SettingsSlider(card, "SFX", new Vector2(0f, 8f), out sfxValue);
        sensitivitySlider = SettingsSlider(card, "SENSITIVITY", new Vector2(0f, -142f), out sensitivityValue);
        Button(card, "BACK", Green, new Vector2(0f, -316f), BackFromSettings, 62f);
    }

    Slider SettingsSlider(RectTransform parent, string label, Vector2 position, out TMP_Text valueText)
    {
        Text(parent, label, 30, Cream, position + new Vector2(-245f, 35f), new Vector2(220f, 48f), true);
        valueText = Text(parent, "70%", 24, Color.white, position + new Vector2(244f, 35f), new Vector2(110f, 48f), true);
        GameObject sliderObject = new GameObject(label + " Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        sliderObject.layer = parent.gameObject.layer;
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(500f, 30f);
        sliderRect.anchoredPosition = position + new Vector2(0f, -12f);
        sliderObject.GetComponent<Image>().color = new Color(0.10f, 0.055f, 0.035f, 1f);

        RectTransform fillArea = ChildRect("Fill Area", sliderRect, new Vector2(0f, 0.2f), new Vector2(1f, 0.8f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
        Image fill = fillArea.gameObject.AddComponent<Image>();
        fill.color = new Color(0.30f, 0.86f, 0.20f, 1f);
        RectTransform handleArea = ChildRect("Handle Slide Area", sliderRect, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
        RectTransform handle = ChildRect("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Cream;
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fillArea;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    void BuildHighScore(RectTransform card)
    {
        Title(card, "HIGH SCORE", 68, new Vector2(0f, 306f), new Vector2(700f, 120f));
        highScoreText = Text(card, "", 34, Cream, new Vector2(0f, 35f), new Vector2(650f, 400f), false);
        Button(card, "BACK", Green, new Vector2(0f, -316f), ShowMainMenu, 62f);
    }

    void BuildHud(RectTransform parent)
    {
        scoreText = AnchoredText(parent, "SCORE 000000", 42, Cream, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -34f), new Vector2(420f, 72f), TextAlignmentOptions.TopLeft);
        timerText = AnchoredText(parent, "TIME ∞", 36, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(250f, 68f), TextAlignmentOptions.Top);
        livesText = AnchoredText(parent, "♥ ♥ ♥", 36, new Color(1f, 0.34f, 0.32f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-138f, -36f), new Vector2(230f, 68f), TextAlignmentOptions.TopRight);
        stageText = AnchoredText(parent, "STAGE 1", 28, Cream, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -93f), new Vector2(360f, 48f), TextAlignmentOptions.TopLeft);
        objectiveText = AnchoredText(parent, "GOAL 100", 23, new Color(1f, 0.72f, 0.35f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -133f), new Vector2(440f, 46f), TextAlignmentOptions.TopLeft);
        comboText = AnchoredText(parent, "", 42, new Color(1f, 0.48f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(380f, 70f), TextAlignmentOptions.Top);
        eventText = AnchoredText(parent, "EVENT", 42, new Color(1f, 0.78f, 0.26f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 145f), new Vector2(900f, 76f), TextAlignmentOptions.Center);
        eventText.enabled = false;
        Button(parent, "II", new Color(0.68f, 0.24f, 0.06f), new Vector2(-52f, -42f), game.PauseGame, 68f, new Vector2(82f, 70f), new Vector2(1f, 1f));
    }

    void BuildPause(RectTransform card)
    {
        Title(card, "PAUSE", 76, new Vector2(0f, 280f), new Vector2(700f, 130f));
        Button(card, "RESUME", Green, new Vector2(0f, 126f), game.ResumeGame);
        Button(card, "RESTART", Orange, new Vector2(0f, 10f), game.RestartRun);
        Button(card, "SETTINGS", new Color(0.53f, 0.24f, 0.09f), new Vector2(0f, -106f), OpenPauseSettings);
        Button(card, "MAIN MENU", new Color(0.18f, 0.38f, 0.72f), new Vector2(0f, -222f), game.ReturnToMenu);
    }

    void BuildGameOver(RectTransform card)
    {
        Title(card, "GAME OVER", 74, new Vector2(0f, 270f), new Vector2(700f, 140f));
        Text(card, "FINAL SCORE", 30, Cream, new Vector2(0f, 142f), new Vector2(600f, 50f), false);
        finalScoreText = Text(card, "0", 72, new Color(1f, 0.65f, 0.12f), new Vector2(0f, 70f), new Vector2(620f, 96f), false);
        Text(card, "BEST SCORE", 27, Cream, new Vector2(0f, -30f), new Vector2(600f, 46f), false);
        bestScoreText = Text(card, "0", 48, Color.white, new Vector2(0f, -88f), new Vector2(620f, 68f), false);
        Button(card, "PLAY AGAIN", Green, new Vector2(0f, -212f), game.RestartRun);
        Button(card, "MAIN MENU", new Color(0.18f, 0.38f, 0.72f), new Vector2(0f, -328f), game.ReturnToMenu, 66f);
    }

    void Button(RectTransform parent, string label, Color color, Vector2 position, Action action, float height = 82f, Vector2? size = null, Vector2? anchor = null)
    {
        Button(parent, label, null, color, position, action, height, size, anchor);
    }

    void Button(RectTransform parent, string label, string subtitle, Color color, Vector2 position, Action action, float height = 82f, Vector2? size = null, Vector2? anchor = null)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.layer = parent.gameObject.layer;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = rect.anchorMax = a;
        rect.pivot = a;
        rect.sizeDelta = size ?? new Vector2(540f, height);
        rect.anchoredPosition = position;
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.006f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -5f);
        Outline buttonOutline = buttonObject.AddComponent<Outline>();
        buttonOutline.effectColor = new Color(1f, 0.68f, 0.25f, 0.82f);
        buttonOutline.effectDistance = new Vector2(1.5f, -1.5f);
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock states = button.colors;
        states.normalColor = color;
        states.highlightedColor = Color.Lerp(color, Color.white, 0.24f);
        states.pressedColor = Color.Lerp(color, Color.black, 0.22f);
        states.selectedColor = states.highlightedColor;
        states.fadeDuration = 0.10f;
        button.colors = states;
        Text(buttonObject.transform, label, subtitle == null ? 34 : 30, Color.white, new Vector2(0f, subtitle == null ? 0f : 12f), new Vector2(rect.sizeDelta.x - 28f, subtitle == null ? height - 6f : 40f), false);
        if (subtitle != null) Text(buttonObject.transform, subtitle, 21, new Color(1f, 0.92f, 0.76f), new Vector2(0f, -19f), new Vector2(rect.sizeDelta.x - 30f, 34f), false);
        button.onClick.AddListener(() => action());
        button.onClick.AddListener(() => { if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayUiClick(); });
        AddHoverSound(button);
    }

    TMP_Text Title(Transform parent, string text, float size, Vector2 position, Vector2 dimensions)
    {
        TMP_Text result = Text(parent, text, size, Cream, position, dimensions, false);
        result.fontStyle = FontStyles.Bold;
        result.outlineWidth = 0.16f;
        result.outlineColor = new Color(0.26f, 0.07f, 0.02f, 1f);
        return result;
    }

    TMP_Text Text(Transform parent, string text, float size, Color color, Vector2 position, Vector2 dimensions, bool leftAligned)
    {
        GameObject go = new GameObject("Text " + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        TMP_Text label = go.GetComponent<TMP_Text>();
        label.text = text;
        label.font = displayFont;
        label.fontSize = size;
        label.color = color;
        label.alignment = leftAligned ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    TMP_Text AnchoredText(Transform parent, string text, float size, Color color, Vector2 min, Vector2 max, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
    {
        TMP_Text label = Text(parent, text, size, color, Vector2.zero, dimensions, false);
        RectTransform rect = label.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = max;
        rect.anchoredPosition = position;
        label.alignment = alignment;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        return label;
    }

    void CreateImage(string name, Transform parent, Color color, bool stretch, Vector2 position, Vector2 size, bool raycast)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        if (stretch) Stretch(rect);
        else { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position; }
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
    }

    void EnsureHudPanels()
    {
        livesText.rectTransform.anchoredPosition = new Vector2(-220f, -36f);
        string[] names = { "HUD Score Panel", "HUD Timer Panel", "HUD Status Panel" };
        foreach (string name in names)
        {
            Transform panel = FindChild(hud.transform, name);
            if (panel != null) panel.gameObject.SetActive(false);
        }
    }

    RectTransform ChildRect(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    void StartModes() => ShowScreen(modesMenu);
    void OpenMainSettings() { settingsReturnToPause = false; ShowScreen(settingsMenu); }
    void OpenPauseSettings() { settingsReturnToPause = true; ShowScreen(settingsMenu); }
    void BackFromSettings() { SaveSettings(); if (settingsReturnToPause) ShowScreen(pauseMenu); else ShowMainMenu(); }

    public void ShowMainMenu() => ShowScreen(mainMenu);
    public void ShowGameplay() { SetScreensVisible(hud); RefreshHud(); }
    public void ShowPause() => ShowScreen(pauseMenu);

    public void ShowGameOver()
    {
        if (game != null)
        {
            finalScoreText.text = game.Score.ToString("N0");
            bestScoreText.text = game.BestScore.ToString("N0");
        }
        ShowScreen(gameOverMenu);
    }

    public void RefreshHud()
    {
        if (game == null || scoreText == null) return;
        if (lastDisplayedScore >= 0 && game.Score != lastDisplayedScore)
        {
            if (scorePulse != null) StopCoroutine(scorePulse);
            scoreText.rectTransform.localScale = Vector3.one;
            if (game.Score > lastDisplayedScore) scorePulse = StartCoroutine(PulseScore());
        }
        lastDisplayedScore = game.Score;
        scoreText.text = "SCORE  " + game.Score.ToString("N0");
        comboText.text = game.Combo > 1 ? "COMBO  x" + game.Combo : string.Empty;
        livesText.text = game.Mode == FruitGameMode.Zen ? "∞" : "♥ × " + Mathf.Max(0, game.Lives).ToString();;
        stageText.text = "STAGE " + game.Stage;
        objectiveText.text = game.BossActive ? "BOSS HITS  " + game.BossHealth :
                             game.SequenceActive ? "ORDER  " + (game.SequenceProgress + 1) + "/3" :
                             "GOAL  " + game.StageGoal.ToString("N0");
        eventText.text = game.EventMessage;
        eventText.enabled = !string.IsNullOrEmpty(game.EventMessage);
        if (game.Mode == FruitGameMode.Arcade)
        {
            int seconds = Mathf.CeilToInt(game.TimeRemaining);
            timerText.text = "TIME  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        else timerText.text = "TIME  " + (ElapsedTime()).ToString("00") + "s";
    }

    int ElapsedTime() => game == null ? 0 : game.ElapsedSeconds;

    IEnumerator PulseScore()
    {
        float elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.unscaledDeltaTime;
            scoreText.rectTransform.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.22f) * Mathf.PI));
            yield return null;
        }
        scoreText.rectTransform.localScale = Vector3.one;
    }

    void ShowHighScore()
    {
        highScoreText.text = "CLASSIC   " + PlayerPrefs.GetInt("FruitNinja_Best_Classic", 0).ToString("N0") +
                             "\nARCADE   " + PlayerPrefs.GetInt("FruitNinja_Best_Arcade", 0).ToString("N0") +
                             "\nZEN   " + PlayerPrefs.GetInt("FruitNinja_Best_Zen", 0).ToString("N0") +
                             "\n\nSTAGE UNLOCKED   " + PlayerPrefs.GetInt("FruitNinja_UnlockedStage", 1) +
                             "\nBEST STREAK   " + PlayerPrefs.GetInt("FruitNinja_BestCombo_Classic", 0) +
                             "\nACHIEVEMENTS   " + (PlayerPrefs.GetInt("FruitNinja_Achievement_Boss", 0) + PlayerPrefs.GetInt("FruitNinja_Achievement_Specialist", 0) + PlayerPrefs.GetInt("FruitNinja_Achievement_Combo10", 0)) + "/3";
        ShowScreen(highScoreMenu);
    }

    void ShowScreen(GameObject screen)
    {
        SetScreensVisible(screen);
        if (screen == null) return;
        CanvasGroup group = screen.GetComponent<CanvasGroup>();
        group.alpha = Application.isPlaying ? 0f : 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        RectTransform card = Array.Find(cards, item => item != null && item.IsChildOf(screen.transform));
        if (card != null) card.localScale = Application.isPlaying ? Vector3.one * 0.94f : Vector3.one;
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(AnimateScreen(group, card));
    }

    IEnumerator AnimateScreen(CanvasGroup group, RectTransform card)
    {
        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float linear = Mathf.Clamp01(elapsed / duration);
            float t = 1f - Mathf.Pow(1f - linear, 3f);
            group.alpha = t;
            if (card != null) card.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, t);
            yield return null;
        }
        group.alpha = 1f;
        if (card != null) card.localScale = Vector3.one;
    }

    void SetScreensVisible(GameObject active)
    {
        if (menuBackdrop != null)
        {
            bool showBackdrop = active != null && active != hud;
            menuBackdrop.gameObject.SetActive(showBackdrop);
            if (showBackdrop)
            {
                menuBackdrop.texture = active == mainMenu ? menuBackgroundTexture : cardWoodTexture;
                menuBackdrop.color = active == gameOverMenu ? new Color(0.72f, 0.48f, 0.42f, 1f) : Color.white;
                UpdateBackdropCrop((float)Screen.width / Mathf.Max(1, Screen.height));
            }
        }
        GameObject[] screens = { mainMenu, modesMenu, settingsMenu, highScoreMenu, hud, pauseMenu, gameOverMenu };
        foreach (GameObject screen in screens)
        {
            if (screen == null) continue;
            bool visible = screen == active;
            screen.SetActive(visible);
            CanvasGroup group = screen.GetComponent<CanvasGroup>();
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }

    void HideLegacyPanels()
    {
        string[] names = { "HoldScoring", "GameOver", "Open P and R " };
        foreach (string name in names)
        {
            Transform found = FindChild(canvas.transform, name);
            if (found != null && found.gameObject != uiRoot.gameObject) found.gameObject.SetActive(false);
        }
    }

    Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform nested = FindChild(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    void LoadSettings()
    {
        FruitNinjaAudio audio = FruitNinjaAudio.Instance;
        float master = audio != null ? audio.MasterVolume : PlayerPrefs.GetFloat("FruitNinja_Master", 1f);
        float music = audio != null ? audio.MusicVolume : PlayerPrefs.GetFloat("FruitNinja_Music", 0.7f);
        float sfx = audio != null ? audio.SfxVolume : PlayerPrefs.GetFloat("FruitNinja_SFX", 0.8f);
        float sensitivity = PlayerPrefs.GetFloat("FruitNinja_Sensitivity", 0.5f);
        masterSlider.SetValueWithoutNotify(master);
        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        masterValue.text = Percent(master); musicValue.text = Percent(music); sfxValue.text = Percent(sfx); sensitivityValue.text = Percent(sensitivity);
        RefreshMuteLabel();
        if (blade != null) blade.SetSensitivity(sensitivity);
    }

    void SaveSettings() { PlayerPrefs.Save(); if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.SavePreferences(); }
    static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
}

[AddComponentMenu("")]
public sealed class ButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    Vector3 target = Vector3.one;
    Vector3 velocity;

    void Update() => transform.localScale = Vector3.SmoothDamp(transform.localScale, target, ref velocity, 0.09f, Mathf.Infinity, Time.unscaledDeltaTime);
    public void OnPointerEnter(PointerEventData _) => target = Vector3.one * 1.06f;
    public void OnPointerExit(PointerEventData _) => target = Vector3.one;
    public void OnPointerDown(PointerEventData _) => target = Vector3.one * 0.94f;
    public void OnPointerUp(PointerEventData _) => target = Vector3.one * 1.06f;
}
