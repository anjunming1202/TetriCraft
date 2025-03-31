using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSoundManager : MonoBehaviour
{
    public AudioClip[] placedSounds;
    public AudioClip[] destroyedSounds;

    private void Awake()
    {
        block = GetComponent<Block>();

        block.OnLockedDown += PlaySoundOnPlaced;
        block.OnDestroyed += PlaySoundOnDestroyed;
    }

    private void PlaySoundOnPlaced()
    {
        Debug.Log("Placed sound on played!");
        if (placedSounds.Length > 0)
        {
            int random = UnityEngine.Random.Range(0, placedSounds.Length);
            AudioManager.Instance.PlaySoundAtPoint(placedSounds[random], block.GetWorldPosition());
        }
    }

    private void PlaySoundOnDestroyed()
    {
        Debug.Log("Destroyed sound on played!");
        if (destroyedSounds.Length > 0)
        {
            int random = UnityEngine.Random.Range(0, destroyedSounds.Length);
            AudioManager.Instance.PlaySoundAtPoint(destroyedSounds[random], block.GetWorldPosition());
        }
    }

    private Block block;
}
