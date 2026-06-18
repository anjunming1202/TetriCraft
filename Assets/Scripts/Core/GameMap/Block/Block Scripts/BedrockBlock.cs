using UnityEngine;

/// <summary>
/// Bedrock garbage block with two-hit destroy behaviour inspired by Minecraft bedrock.
///
/// First destroy attempt: 50 % chance to succeed outright; on failure the block becomes
/// "cracked" and the attempt is cancelled.
/// While cracked: the next destroy attempt is guaranteed to succeed.
///
/// Visual feedback for the cracked state is not yet implemented.
/// </summary>
public class BedrockBlock : Block
{
    public override BlockID ID => BlockID.Bedrock;

    private bool _cracked = false;

    /// <summary>Whether this block has survived one destroy attempt and is now cracked.</summary>
    public bool IsCracked => _cracked;

    /// <summary>
    /// Returns whether this destroy attempt actually goes through.
    /// First hit: 50 % chance. On failure, marks the block as cracked and notifies the renderer.
    /// Cracked block: always destroyed.
    /// </summary>
    public override bool CanBeDestroyed()
    {
        if (_cracked)
            return true;

        bool succeeds = Random.value < 0.5f;
        if (!succeeds)
        {
            _cracked = true;
            OnTriggerAppearanceChanged();
        }
        return succeeds;
    }
}
