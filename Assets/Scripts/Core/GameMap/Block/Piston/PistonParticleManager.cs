using static Unity.Collections.AllocatorManager;
using UnityEngine;

public class PistonParticleManager : BlockParticleManager
{
    protected override void Awake()
    {
        block = GetComponent<Block>();
        block.OnAfterDestroyed += SpawnBreakingParticles;

        baseRenderer = transform.Find("base").GetComponent<SpriteRenderer>();
        headRenderer = transform.Find("head").GetComponent<SpriteRenderer>();
    }

    protected override void SpawnBreakingParticles(Block b)
    {
        SpawnBreakingParticles(baseRenderer.transform.position, baseRenderer.sprite.texture);
        SpawnBreakingParticles(headRenderer.transform.position, headRenderer.sprite.texture);
    }

    private SpriteRenderer baseRenderer;
    private SpriteRenderer headRenderer;
}
