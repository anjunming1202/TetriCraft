using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    // Entity system, currently just a list
    private readonly List<Entity> entities = new List<Entity>();

    // batch updates
    private readonly Dictionary<Entity, Vector2> spawnedEntityBatch = new();
    private readonly HashSet<Entity> killedEntityBatch = new();

    // map reference
    private MapManager MapManager;

    public void Init(MapManager mapManager)
    {
        // Map reference
        this.MapManager = mapManager;
    }

    public void Clear()
    {
        // clear entity system
        foreach (Entity entity in entities)
        {
            if (entity != null)
                entity.Die();
        }
        entities.Clear();
        spawnedEntityBatch.Clear();
        killedEntityBatch.Clear();
    }

    public void OnUpdate()
    {
        foreach (var entity in entities)
        {
            if (entity != null)
                entity.OnTickUpdate(Time.deltaTime);
        }
        ProcessPendingEntityRequests();
    }

    public void RequestAddEntity(Entity entity, float x, float y)
    {
        spawnedEntityBatch.Add(entity, new(x, y));
    }

    public void RequestKillEntity(Entity entity)
    {
        killedEntityBatch.Add(entity);
    }

    private void ProcessPendingEntityRequests()
    {
        foreach (var (entity, position) in spawnedEntityBatch)
        {
            ApplySpawn(entity, position);
        }
        foreach (var entity in killedEntityBatch)
        {
            ApplyKill(entity);
        }

        spawnedEntityBatch.Clear();
        killedEntityBatch.Clear();
    }

    private void ApplySpawn(Entity entity, Vector2 pos)
    {
        // spawn entity
        entity.OnSpawned(MapManager, pos);

        // add to entity list
        entities.Add(entity);

        // reparent the entity object
        entity.transform.SetParent(this.transform, true);
    }
    private void ApplyKill(Entity entity)
    {
        // kill entity
        entity.Die();

        // remove from entity list
        entities.Remove(entity);
    }
}