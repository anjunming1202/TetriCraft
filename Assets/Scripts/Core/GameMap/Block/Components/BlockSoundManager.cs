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
        block.OnAfterDestroyed += PlaySoundOnDestroyed;
    }

    private void PlaySoundOnPlaced(Block b)
    {
        // Check before the Random.Range draw below, not just before AudioManager use — the draw
        // itself would perturb the piece RNG stream.
        if (HeadlessRuntime.IsHeadless)
            return;

        if (placedSounds.Length > 0)
        {
            int random = UnityEngine.Random.Range(0, placedSounds.Length);
            AudioManager.Instance.PlaySFXAtPoint(placedSounds[random], block.GetWorldPosition(), 1f, AudioBus.Block);
        }
    }

    private void PlaySoundOnDestroyed(Block b)
    {
        if (HeadlessRuntime.IsHeadless)
            return;

        if (destroyedSounds.Length > 0)
        {
            int random = UnityEngine.Random.Range(0, destroyedSounds.Length);
            AudioManager.Instance.PlaySFXAtPoint(destroyedSounds[random], block.GetWorldPosition(), 1f, AudioBus.Block);
        }
    }

    private Block block;
}
