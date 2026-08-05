using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [SerializeField] private Transform particleRoot;

    private readonly List<GameObject> _spawnedParticles = new();

    public ParticleSystem SpawnParticle(ParticleSystem prefab, Vector3 worldPosition, Quaternion rotation = default, Transform parentOverride = null)
    {
        if (HeadlessRuntime.IsHeadless)
            return null;

        if (prefab == null)
        {
            Debug.LogWarning("ParticleManager.SpawnParticle called with null prefab.");
            return null;
        }

        Transform parent = parentOverride != null ? parentOverride : particleRoot;

        ParticleSystem instance = Instantiate(prefab, worldPosition, rotation, parent);
        GameObject go = instance.gameObject;

        _spawnedParticles.Add(go);

        instance.Play(true);

        return instance;
    }

    public ParticleSystem SpawnParticle(ParticleSystem prefab, Vector3 worldPosition)
    {
        return SpawnParticle(prefab, worldPosition, Quaternion.identity, null);
    }

    public void DespawnParticle(ParticleSystem particle)
    {
        if (particle == null)
        {
            return;
        }

        GameObject go = particle.gameObject;

        if (_spawnedParticles.Remove(go))
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(go);
        }
        else
        {
            Destroy(go);
        }
    }

    public void ClearAll()
    {
        for (int i = _spawnedParticles.Count - 1; i >= 0; i--)
        {
            GameObject go = _spawnedParticles[i];
            if (go != null)
            {
                ParticleSystem ps = go.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                Destroy(go);
            }
        }

        _spawnedParticles.Clear();
    }

    public void Dispose()
    {
        ClearAll();
    }

    private void OnDisable()
    {
        // In case the map is disabled before explicit cleanup.
        ClearAll();
    }
}