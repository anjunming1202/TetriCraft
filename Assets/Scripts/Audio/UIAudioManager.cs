using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : PersistentSingleton<UIAudioManager>
{
    AudioSource clickSource;

    protected override void Awake()
    {
        base.Awake();
        clickSource = GetComponent<AudioSource>();
    }

    public void Play()
    {
        clickSource.Play();
    }
}
