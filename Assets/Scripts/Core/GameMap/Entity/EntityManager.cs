using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    // Entity system, currently just a list
    private List<Entity> entities = new List<Entity>();

    // map reference
    private MapManager MapManager;

    public void Init(MapManager mapManager)
    {
        // Map reference
        this.MapManager = mapManager;

        // Init entity system
        entities = new();
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
    }

    public void OnUpdate()
    {
        foreach (var entity in entities)
        {
            if (entity != null)
                entity.TickUpdate(Time.deltaTime);
        }
    }

    public void AddNewEntity(Entity entity, float x, float y)
    {
        // spawn entity
        entity.OnSpawned(MapManager, new(x, y));

        // add to entity list
        entities.Add(entity);
    }

    public void KillEntity(Entity entity)
    {
        // kill entity
        entity.Die();

        // remove from entity list
        entities.Remove(entity);
    }
}