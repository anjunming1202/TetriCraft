using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSoundManager : MonoBehaviour
{
    public AudioClip[] digSounds;

    private void Awake()
    {
        block = GetComponent<Block>();
        placedSounds = digSounds;
        destroyedSounds = digSounds;

        block.OnLockedDown += PlaySoundOnPlaced;
        block.OnDestroyed += PlaySoundOnDestroyed;
    }

    private void PlaySoundOnPlaced()
    {
        Debug.Log("Placed sound on played!");
        if (placedSounds.Length > 0)
        {
            int random = UnityEngine.Random.Range(0, placedSounds.Length);
            AudioManager.Instance.PlaySound(placedSounds[random], block.GetWorldPosition());
        }
    }

    private void PlaySoundOnDestroyed()
    {
        Debug.Log("Destroyed sound on played!");
        if (destroyedSounds.Length > 0)
        {
            int random = UnityEngine.Random.Range(0, destroyedSounds.Length);
            AudioManager.Instance.PlaySound(destroyedSounds[random], block.GetWorldPosition());
        }
    }

    private Block block;

    private AudioClip[] placedSounds;
    private AudioClip[] destroyedSounds;
}
