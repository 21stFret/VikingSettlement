using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

public enum SFX
{
    // UI
    UIMove,
    UISelect,
    UIBack,
    UIConfirm,
    UIError,

    // Combat
    SwordSwing,
    SwordHit,
    ShieldBlock,
    BowShoot,
    ArrowHit,

    // Actions
    Footstep,
    Build,
    Chop,
    Mine,
    Harvest,
    Pickup,
    Drop,

    // Feedback
    LevelUp,
    QuestComplete,
    Death
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource ambientSource;

    [Header("Music Tracks")]
    public AudioClip[] peacefulTracks;
    public AudioClip[] combatTracks;
    public AudioClip[] menuTracks;

    [Header("Sound Effects")]
    public AudioClip[] sfxClips;

    [Header("Ambient")]
    public AudioClip[] ambientDaytime;
    public AudioClip[] ambientNighttime;

    [Header("Mixer")]
    public AudioMixer audioMixer;

    [Header("Volume Settings")]
    [Range(0, 1)] public float musicVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;
    [Range(0, 1)] public float ambientVolume = 0.5f;

    [Header("Settings")]
    public float musicFadeDuration = 1f;

    private AudioClip pendingMusicClip;
    private Coroutine ambientCoroutine;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string AMBIENT_VOLUME_KEY = "AmbientVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadVolumeSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        ambientVolume = PlayerPrefs.GetFloat(AMBIENT_VOLUME_KEY, 0.5f);

        ApplyMusicVolume();
        ApplySFXVolume();
        ApplyAmbientVolume();
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.SetFloat(AMBIENT_VOLUME_KEY, ambientVolume);
        PlayerPrefs.Save();
    }

    #region Music

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null) return;

        if (fade && musicSource.isPlaying)
        {
            pendingMusicClip = clip;
            FadeMusicOut();
        }
        else
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void PlayPeacefulMusic(int trackIndex = -1)
    {
        if (peacefulTracks == null || peacefulTracks.Length == 0) return;

        if (trackIndex < 0 || trackIndex >= peacefulTracks.Length)
            trackIndex = Random.Range(0, peacefulTracks.Length);

        PlayMusic(peacefulTracks[trackIndex]);
    }

    public void PlayCombatMusic(int trackIndex = -1)
    {
        if (combatTracks == null || combatTracks.Length == 0) return;

        if (trackIndex < 0 || trackIndex >= combatTracks.Length)
            trackIndex = Random.Range(0, combatTracks.Length);

        PlayMusic(combatTracks[trackIndex]);
    }

    public void PlayMenuMusic(int trackIndex = -1)
    {
        if (menuTracks == null || menuTracks.Length == 0) return;

        if (trackIndex < 0 || trackIndex >= menuTracks.Length)
            trackIndex = Random.Range(0, menuTracks.Length);

        PlayMusic(menuTracks[trackIndex]);
    }

    public void StopMusic(bool fade = true)
    {
        if (fade)
        {
            pendingMusicClip = null;
            FadeMusicOut();
        }
        else
        {
            musicSource.Stop();
        }
    }

    private void FadeMusicOut()
    {
        if (audioMixer != null)
        {
            DOVirtual.Float(musicVolume, 0.0001f, musicFadeDuration,
                v => audioMixer.SetFloat("MusicVolume", Mathf.Log10(v) * 20))
                .OnComplete(OnMusicFadeOutComplete);
        }
        else
        {
            musicSource.DOFade(0f, musicFadeDuration).OnComplete(OnMusicFadeOutComplete);
        }
    }

    private void OnMusicFadeOutComplete()
    {
        musicSource.Stop();

        if (pendingMusicClip != null)
        {
            musicSource.clip = pendingMusicClip;
            musicSource.Play();
            pendingMusicClip = null;
            FadeMusicIn();
        }
    }

    private void FadeMusicIn()
    {
        if (audioMixer != null)
        {
            DOVirtual.Float(0.0001f, musicVolume, musicFadeDuration,
                v => audioMixer.SetFloat("MusicVolume", Mathf.Log10(v) * 20));
        }
        else
        {
            musicSource.volume = 0f;
            musicSource.DOFade(musicVolume, musicFadeDuration);
        }
    }

    #endregion

    #region Sound Effects

    public void PlaySFX(SFX sfx)
    {
        int index = (int)sfx;
        if (sfxClips == null || index < 0 || index >= sfxClips.Length) return;
        if (sfxClips[index] == null) return;

        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(sfxClips[index]);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXWithPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (clip == null) return;
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume);
    }

    #endregion

    #region Ambient

    public void PlayAmbient(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        ambientSource.clip = clip;
        ambientSource.loop = loop;
        ambientSource.Play();
    }

    public void PlayDaytimeAmbient()
    {
        if (ambientDaytime == null || ambientDaytime.Length == 0) return;
        PlayAmbient(ambientDaytime[Random.Range(0, ambientDaytime.Length)]);
    }

    public void PlayNighttimeAmbient()
    {
        if (ambientNighttime == null || ambientNighttime.Length == 0) return;
        PlayAmbient(ambientNighttime[Random.Range(0, ambientNighttime.Length)]);
    }

    public void StopAmbient()
    {
        ambientSource.Stop();
    }

    #endregion

    #region Volume Control

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume();
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplySFXVolume();
        SaveVolumeSettings();
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        ApplyAmbientVolume();
        SaveVolumeSettings();
    }

    private void ApplyMusicVolume()
    {
        if (audioMixer != null)
        {
            float dbVolume = musicVolume > 0.0001f ? Mathf.Log10(musicVolume) * 20 : -80f;
            audioMixer.SetFloat("MusicVolume", dbVolume);
        }
        else if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    private void ApplySFXVolume()
    {
        if (audioMixer != null)
        {
            float dbVolume = sfxVolume > 0.0001f ? Mathf.Log10(sfxVolume) * 20 : -80f;
            audioMixer.SetFloat("SFXVolume", dbVolume);
        }
        else if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    private void ApplyAmbientVolume()
    {
        if (audioMixer != null)
        {
            float dbVolume = ambientVolume > 0.0001f ? Mathf.Log10(ambientVolume) * 20 : -80f;
            audioMixer.SetFloat("AmbientVolume", dbVolume);
        }
        else if (ambientSource != null)
        {
            ambientSource.volume = ambientVolume;
        }
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetAmbientVolume() => ambientVolume;

    #endregion
}
