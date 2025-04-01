using System.Collections;
using UnityEngine;
using static Unity.Collections.AllocatorManager;


public class BlockParticleManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem breakingParticles;

    private void Awake()
    {
        block = GetComponent<Block>();
        blockRenderer = GetComponent<BlockRenderer>();

        block.OnDestroyed += SpawnBreakingParticles;
    }

    private void SpawnBreakingParticles()
    {
        breakingParticlesInstance = Instantiate(breakingParticles, transform.position, Quaternion.identity);
        breakingParticlesRenderer = breakingParticlesInstance.GetComponent<ParticleSystemRenderer>();
        Material material = new Material(breakingParticlesRenderer.material);
        material.mainTexture = blockRenderer.Texture;
        breakingParticlesRenderer.material = material;
    } // Todo: currently using new instantiated material, increasing draw call

    private Block block;
    private BlockRenderer blockRenderer;

    private ParticleSystem breakingParticlesInstance;
    private ParticleSystemRenderer breakingParticlesRenderer;
}