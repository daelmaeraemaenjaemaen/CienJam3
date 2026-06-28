using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource heartbeatAudioSource;

    [Header("Default Clips")]
    [SerializeField] private AudioClip defaultBGMClip;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private bool playDefaultBGMOnStart = true;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float heartbeatVolume = 1f;

    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public float HeartbeatVolume => heartbeatVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigureAudioSources();
        ApplyVolumes();
    }

    private void Start()
    {
        if (playDefaultBGMOnStart && defaultBGMClip != null)
            PlayBGM(defaultBGMClip);
    }

    public void PlayBGM(AudioClip clip)
    {
        PlayBGM(clip, true);
    }

    public void PlayBGM(AudioClip clip, bool loop)
    {
        if (bgmAudioSource == null || clip == null)
            return;

        if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying && bgmAudioSource.loop == loop)
            return;

        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = loop;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        if (bgmAudioSource == null)
            return;

        bgmAudioSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxAudioSource == null || clip == null)
            return;

        sfxAudioSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayHeartbeat()
    {
        if (heartbeatAudioSource == null || heartbeatClip == null)
            return;

        heartbeatAudioSource.clip = heartbeatClip;
        heartbeatAudioSource.loop = true;
        heartbeatAudioSource.volume = heartbeatVolume;

        if (!heartbeatAudioSource.isPlaying)
            heartbeatAudioSource.Play();
    }

    public void StopHeartbeat()
    {
        if (heartbeatAudioSource == null)
            return;

        heartbeatAudioSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmAudioSource != null)
            bgmAudioSource.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (sfxAudioSource != null)
            sfxAudioSource.volume = sfxVolume;
    }

    public void SetHeartbeatVolume(float volume)
    {
        heartbeatVolume = Mathf.Clamp01(volume);

        if (heartbeatAudioSource != null)
            heartbeatAudioSource.volume = heartbeatVolume;
    }

    private void ConfigureAudioSources()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
        }

        if (sfxAudioSource != null)
        {
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
        }

        if (heartbeatAudioSource != null)
        {
            heartbeatAudioSource.playOnAwake = false;
            heartbeatAudioSource.loop = true;

            if (heartbeatClip != null)
                heartbeatAudioSource.clip = heartbeatClip;
        }
    }

    private void ApplyVolumes()
    {
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);
        SetHeartbeatVolume(heartbeatVolume);
    }
}
