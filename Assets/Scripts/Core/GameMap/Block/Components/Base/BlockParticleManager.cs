using System.Collections;
using UnityEngine;
using static Unity.Collections.AllocatorManager;


public class BlockParticleManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem breakingParticles;

    protected virtual void Awake()
    {
        block = GetComponent<Block>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        block.OnDestroyed += SpawnBreakingParticles;
    }

    protected virtual void SpawnBreakingParticles()
    {
        SpawnBreakingParticles(transform.position, spriteRenderer.sprite.texture);
    }

    protected void SpawnBreakingParticles(Vector3 position, Texture texture)
    {
        if (breakingParticles == null)
            return;

        breakingParticlesInstance = Instantiate(breakingParticles, position, Quaternion.identity);
        breakingParticlesRenderer = breakingParticlesInstance.GetComponent<ParticleSystemRenderer>();
        Material material = new Material(breakingParticlesRenderer.material);
        material.mainTexture = texture;
        breakingParticlesRenderer.material = material;
    } // Todo: currently using new instantiated material, increasing draw call

    protected Block block;
    private SpriteRenderer spriteRenderer;

    private ParticleSystem breakingParticlesInstance;
    private ParticleSystemRenderer breakingParticlesRenderer;
}