using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioManager: Singleton persistent manager for music and SFX
/// Features:
/// - Master / Music / SFX volume groups (supports AudioMixer if provided)
/// - Save / Load volume settings via PlayerPrefs
/// - PlayMusic with optional crossfade
/// - PlaySFX with pooling for concurrency
/// - Helper methods for UI binding (setters accepting 0..1 float)
/// 
/// Usage:
/// - Place this script on a GameObject in your initial scene and mark as DontDestroyOnLoad.
/// - Optionally assign an AudioMixer with exposed parameters: "MasterVolume", "MusicVolume", "SFXVolume".
/// - Hook UI sliders to SetMasterVolumeNormalized/SetMusicVolumeNormalized/SetSFXVolumeNormalized (values 0..1).
/// </summary>
public class AudioManager : PersistentSingleton<AudioManager>
{
    [Header("Mixer (optional)")]
    [Tooltip("Optional AudioMixer. If assigned, the manager will try to set exposed params MasterVolume, MusicVolume and SFXVolume (in dB).")]
    public AudioMixer mixer;

    [Header("Music")]
    public AudioSource musicSourceA;
    public AudioSource musicSourceB; // used for crossfade
    [Tooltip("Default crossfade duration in seconds")]
    public float defaultMusicCrossfade = 1.0f;

    [Header("SFX")]
    public AudioSource sfxSourcePrefab;
    [Tooltip("Maximum simultaneous SFX sources to keep in pool")]
    public int sfxInitialPoolSize = 16;

    // Internal pool
    private readonly Queue<AudioSource> sfxPool = new Queue<AudioSource>();

    // Volume state stored as linear 0..1 values
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    // mixer parameter names expected if mixer is provided
    private const string MIXER_MASTER_PARAM = "MasterVolume";
    private const string MIXER_MUSIC_PARAM = "MusicVolume";
    private const string MIXER_SFX_PARAM = "SFXVolume";

    // used for crossfading
    private AudioSource activeMusicSource;
    private AudioSource fadingMusicSource;

    protected override void Awake()
    {
        base.Awake();
        
        // Validate / create music sources if missing
        if (musicSourceA == null)
        {
            musicSourceA = gameObject.AddComponent<AudioSource>();
            musicSourceA.playOnAwake = false;
            musicSourceA.loop = true;
        }
        if (musicSourceB == null)
        {
            musicSourceB = gameObject.AddComponent<AudioSource>();
            musicSourceB.playOnAwake = false;
            musicSourceB.loop = true;
        }

        activeMusicSource = musicSourceA;
        fadingMusicSource = musicSourceB;

        // Build SFX pool
        BuildSFXPool();
    }

    /// <summary>
    /// Play sound in the world
    /// </summary>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume * masterVolume);
    }

    /// <summary>
    /// Play a one-shot SFX clip at the manager's pool. Returns true if played.
    /// </summary>
    public bool PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxPool.Count == 0)
        {
            // no pool available
            var fallback = gameObject.AddComponent<AudioSource>();
            fallback.PlayOneShot(clip, volume * sfxVolume * masterVolume);
            //Destroy(fallback, clip.length + 0.1f);
            return true;
        }

        // dequeue from pool
        var src = sfxPool.Dequeue();

        // configure
        src.clip = clip;
        src.volume = volume * sfxVolume * masterVolume;
        src.loop = false;
        src.Play();

        // enqueue after playing
        StartCoroutine(ReturnSFXToPoolWhenDone(src, clip.length / Mathf.Abs(src.pitch)));
        return true;
    }

    /// <summary>
    /// Play sound following an object
    /// </summary>
    public void PlaySFXFollowing(AudioClip clip, Transform target, float volume = 1f)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = target.position;
        tempGO.transform.parent = target;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.volume = volume;
        aSource.spatialBlend = 0f; // 2D audio
        aSource.Play();

        GameObject.Destroy(tempGO, clip.length);
    }

    private IEnumerator ReturnSFXToPoolWhenDone(AudioSource src, float wait)
    {
        yield return new WaitForSeconds(wait + 0.05f);
        // clear clip reference so that inspector doesn't show references
        src.clip = null;
        sfxPool.Enqueue(src);
    }

    /// <summary>
    /// Play a music clip. If crossfadeDuration > 0, crossfades to the new clip.
    /// </summary>
    public void PlayMusic(AudioClip clip, float crossfadeDuration = -1f, bool loop = true)
    {
        if (crossfadeDuration < 0f) crossfadeDuration = defaultMusicCrossfade;

        // if no crossfade (duration == 0) simply switch
        if (crossfadeDuration <= 0f)
        {
            activeMusicSource.Stop();
            activeMusicSource.clip = clip;
            activeMusicSource.loop = loop;
            activeMusicSource.volume = musicVolume * masterVolume;
            activeMusicSource.Play();
            return;
        }

        // crossfade: assign clip to fading source and gradually swap volumes
        fadingMusicSource.clip = clip;
        fadingMusicSource.loop = loop;
        fadingMusicSource.volume = 0f;
        fadingMusicSource.Play();

        StopAllCoroutines(); // stop any ongoing crossfades
        StartCoroutine(CrossfadeMusicCoroutine(activeMusicSource, fadingMusicSource, crossfadeDuration));

        // swap references
        var temp = activeMusicSource;
        activeMusicSource = fadingMusicSource;
        fadingMusicSource = temp;
    }

    private IEnumerator CrossfadeMusicCoroutine(AudioSource from, AudioSource to, float duration)
    {
        float t = 0f;
        float fromStartVol = (from != null) ? from.volume : 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            if (from != null) from.volume = Mathf.Lerp(fromStartVol, 0f, p) * masterVolume * musicVolume;
            if (to != null) to.volume = Mathf.Lerp(0f, 1f, p) * masterVolume * musicVolume;
            yield return null;
        }

        if (from != null)
        {
            from.Stop();
            from.volume = 0f;
            from.clip = null;
        }

        if (to != null) to.volume = musicVolume * masterVolume;
    }

    /// <summary>
    /// Stop music immediately.
    /// </summary>
    public void StopMusic()
    {
        if (activeMusicSource != null) activeMusicSource.Stop();
        if (fadingMusicSource != null) fadingMusicSource.Stop();
    }

    public void LoadAndApply()
    {
        /*// Load saved volumes
        LoadVolumes();

        // Apply loaded volumes to mixer / sources
        ApplyVolumesToAudioSystem();*/
    }

    /// <summary>
    /// Set master volume normalized 0..1 (use from UI slider)
    /// This updates mixer if present and saves to PlayerPrefs.
    /// </summary>
    public void SetMasterVolumeNormalized(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        if (mixer != null)
            mixer.SetFloat(MIXER_MASTER_PARAM, LinearToDecibel(masterVolume));
    }

    /// <summary>
    /// Set music volume normalized 0..1
    /// </summary>
    public void SetMusicVolumeNormalized(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        if (mixer != null)
            mixer.SetFloat(MIXER_MUSIC_PARAM, LinearToDecibel(musicVolume));
    }

    /// <summary>
    /// Set SFX volume normalized 0..1
    /// </summary>
    public void SetSFXVolumeNormalized(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        if (mixer != null)
            mixer.SetFloat(MIXER_SFX_PARAM, LinearToDecibel(sfxVolume));
    }

    /// <summary>
    /// Exposed getters (normalized 0..1) useful for UI sliders to read initial values
    /// </summary>
    public float GetMasterVolumeNormalized() => masterVolume;
    public float GetMusicVolumeNormalized() => musicVolume;
    public float GetSFXVolumeNormalized() => sfxVolume;

    private void BuildSFXPool()
    {
        // if no prefab assigned, create default children audio sources
        if (sfxSourcePrefab == null)
        {
            for (int i = 0; i < sfxInitialPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                sfxPool.Enqueue(src);
            }
            return;
        }

        // if prefab provided, instantiate a simple pool under this manager
        for (int i = 0; i < sfxInitialPoolSize; i++)
        {
            var inst = Instantiate(sfxSourcePrefab, transform);
            inst.playOnAwake = false;
            sfxPool.Enqueue(inst);
        }
    }

    private void ApplyVolumesToAudioSystem()
    {
        if (mixer != null)
        {
            mixer.SetFloat(MIXER_MASTER_PARAM, LinearToDecibel(masterVolume));
            mixer.SetFloat(MIXER_MUSIC_PARAM, LinearToDecibel(musicVolume));
            mixer.SetFloat(MIXER_SFX_PARAM, LinearToDecibel(sfxVolume));
        }
    }

    /// <summary>
    /// Convert linear 0..1 value to AudioMixer dB value (logarithmic). Use -80 dB for zero.
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        linear = Mathf.Clamp01(linear);
        if (linear <= 0.0001f) return -80f; // effectively silent
        return Mathf.Log10(linear) * 20f;
    }

    /// <summary>
    /// Example debug helpers
    /// </summary>
    [ContextMenu("ResetVolumesToDefault")]
    public void ResetVolumesToDefault()
    {
        SetMasterVolumeNormalized(1f);
        SetMusicVolumeNormalized(1f);
        SetSFXVolumeNormalized(1f);
    }
}
