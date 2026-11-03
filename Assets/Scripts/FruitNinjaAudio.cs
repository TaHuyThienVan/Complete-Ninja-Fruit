using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class FruitNinjaAudio : MonoBehaviour
{
    public static FruitNinjaAudio Instance { get; private set; }

    const string MasterKey = "FruitNinja_Master";
    const string MusicKey = "FruitNinja_Music";
    const string SfxKey = "FruitNinja_SFX";
    const string MuteKey = "FruitNinja_Muted";

    AudioMixer mixer;
    AudioMixerGroup musicGroup, sfxGroup;
    readonly AudioSource[] music = new AudioSource[2];
    readonly AudioSource[] sfx = new AudioSource[12];
    readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();
    readonly AudioClip[] slices = new AudioClip[2];
    AudioClip menuMusic, gameplayMusic, bossMusic, gameOverMusic;
    AudioClip whoosh, splash, combo, bomb, bombWarning, powerUp, levelUp, achievement, lifeLoss;
    AudioClip uiHover, uiClick, uiConfirm;
    int musicIndex, sfxIndex;
    Coroutine crossfade;
    float masterVolume, musicVolume, sfxVolume;
    bool muted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        mixer = Resources.Load<AudioMixer>("Audio/FruitNinjaMixer");
        if (mixer != null)
        {
            AudioMixerGroup[] mg = mixer.FindMatchingGroups("Music");
            AudioMixerGroup[] sg = mixer.FindMatchingGroups("SFX");
            if (mg.Length > 0) musicGroup = mg[0];
            if (sg.Length > 0) sfxGroup = sg[0];
        }

        for (int i = 0; i < music.Length; i++) music[i] = MakeSource("Music " + i, musicGroup);
        for (int i = 0; i < sfx.Length; i++) sfx[i] = MakeSource("SFX " + i, sfxGroup);

        menuMusic = Load("Music_Menu");
        gameplayMusic = Load("Music_Gameplay");
        bossMusic = Load("Music_Boss");
        gameOverMusic = Load("Music_GameOver");
        slices[0] = Load("SFX_Fruit_Slice_01");
        slices[1] = Load("SFX_Fruit_Slice_02");
        whoosh = Load("SFX_Whoosh_01");
        splash = Load("SFX_Fruit_Splash");
        combo = Load("SFX_Combo");
        bomb = Load("SFX_Bomb_Explosion");
        bombWarning = Load("SFX_Bomb_Warning");
        powerUp = Load("SFX_PowerUp");
        levelUp = Load("SFX_LevelUp");
        achievement = Load("SFX_Achievement");
        lifeLoss = Load("SFX_LifeLoss");
        uiHover = Load("SFX_UI_Hover");
        uiClick = Load("SFX_UI_Click");
        uiConfirm = Load("SFX_UI_Confirm");

        masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.8f);
        muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
        ApplyVolumes();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnApplicationPause(bool paused) { if (paused) SavePreferences(); }
    void OnApplicationQuit() => SavePreferences();

    AudioSource MakeSource(string sourceName, AudioMixerGroup group)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = group;
        return source;
    }

    static AudioClip Load(string name) => Resources.Load<AudioClip>("Audio/" + name);

    public void PlayMenuMusic() => PlayMusic(menuMusic, true);
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic, true);
    public void PlayBossMusic() => PlayMusic(bossMusic, true);
    public void PlayGameOverMusic() => PlayMusic(gameOverMusic, false);

    void PlayMusic(AudioClip clip, bool loop)
    {
        if (clip == null) return;
        AudioSource current = music[musicIndex];
        if (current.clip == clip && current.isPlaying) return;
        if (crossfade != null) StopCoroutine(crossfade);
        int nextIndex = 1 - musicIndex;
        AudioSource next = music[nextIndex];
        next.Stop();
        next.clip = clip;
        next.loop = loop;
        bool fading = current.isPlaying;
        next.volume = fading ? musicVolume * 0.15f : musicVolume;
        next.Play();
        if (!fading)
        {
            musicIndex = nextIndex;
            return;
        }
        crossfade = StartCoroutine(Crossfade(current, next, nextIndex));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, int nextIndex)
    {
        const float duration = 0.65f;
        float elapsed = 0f;
        float start = from.volume;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            from.volume = start * (1f - t);
            to.volume = musicVolume * t;
            yield return null;
        }
        from.Stop();
        from.volume = 0f;
        to.volume = musicVolume;
        musicIndex = nextIndex;
        crossfade = null;
    }

    public void PlayWhoosh() => Play(whoosh, 0.55f, 0.04f);
    public void PlaySlice() { Play(slices[Random.Range(0, slices.Length)], 0.85f, 0.035f); Play(splash, 0.35f, 0.04f); }
    public void PlayCombo() => Play(combo, 0.75f, 0.25f);
    public void PlayBomb() => Play(bomb, 0.9f, 0.12f);
    public void PlayBombWarning() => Play(bombWarning, 0.5f, 0.5f);
    public void PlayPowerUp() => Play(powerUp, 0.8f, 0.1f);
    public void PlayLevelUp() => Play(levelUp, 0.7f, 0.1f);
    public void PlayAchievement() => Play(achievement, 0.8f, 0.2f);
    public void PlayLifeLoss() => Play(lifeLoss, 0.85f, 0.18f);
    public void PlayUiHover() => Play(uiHover, 0.28f, 0.08f);
    public void PlayUiClick() => Play(uiClick, 0.55f, 0.04f);
    public void PlayUiConfirm() => Play(uiConfirm, 0.65f, 0.08f);

    void Play(AudioClip clip, float volume, float cooldown)
    {
        if (clip == null || muted || sfxVolume <= 0f) return;
        if (lastPlayed.TryGetValue(clip, out float last) && Time.unscaledTime - last < cooldown) return;
        lastPlayed[clip] = Time.unscaledTime;
        AudioSource source = sfx[sfxIndex++ % sfx.Length];
        source.pitch = Random.Range(0.96f, 1.04f);
        source.PlayOneShot(clip, volume * sfxVolume);
    }

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public bool Muted => muted;

    public void SetMasterVolume(float value) { masterVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MasterKey, masterVolume); ApplyVolumes(); }
    public void SetMusicVolume(float value) { musicVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, musicVolume); if (crossfade == null && music[musicIndex] != null && music[musicIndex].isPlaying) music[musicIndex].volume = musicVolume; }
    public void SetSfxVolume(float value) { sfxVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfxVolume); }
    public void SetMuted(bool value) { muted = value; PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0); ApplyVolumes(); PlayerPrefs.Save(); }

    void ApplyVolumes()
    {
        AudioListener.volume = muted ? 0f : masterVolume;
        if (crossfade == null && music[musicIndex] != null && music[musicIndex].isPlaying) music[musicIndex].volume = musicVolume;
    }

    public void SavePreferences() => PlayerPrefs.Save();
}
