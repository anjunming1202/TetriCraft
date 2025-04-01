using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public void PlaySoundAtPoint(AudioClip clip, Vector3 position)
    {
        AudioSource.PlayClipAtPoint(clip, position);
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
