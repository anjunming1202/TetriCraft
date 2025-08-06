using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private int initialPoolSize = 5;
    private List<AudioSource> audioSourcePool; // globle audio sources

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play sound in the world
    /// </summary>
    public void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    /// <summary>
    /// Play global sound
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f, bool loop = false)
    {
        AudioSource source = GetAvailableAudioSource();
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
        source.Play();
    }

    /// <summary>
    /// Play sound following an object
    /// </summary>
    public void PlaySoundFollowed(AudioClip clip, Transform target, float volume = 1f)
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

    private void InitializePool()
    {
        audioSourcePool = new List<AudioSource>();
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.spatialBlend = 0f; // 2D audio
        newSource.playOnAwake = false;
        audioSourcePool.Add(newSource);
        return newSource;
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource source in audioSourcePool)
        {
            if (!source.isPlaying) return source;
        }

        return CreateNewAudioSource();
    }
}
