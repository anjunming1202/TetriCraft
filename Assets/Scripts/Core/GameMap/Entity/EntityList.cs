using System.Collections.Generic;
using UnityEngine;

public class EntityList : MonoBehaviour
{
    private List<Entity> entities;
    public List<Entity> Entities => entities;

    public void Init()
    {
        entities = new List<Entity>();
    }

    public void Add(Entity entity)
    {
        entities.Add(entity);
    }

    public void Remove(Entity entity)
    {
        entities.Remove(entity);
    }

    public void RemoveAll()
    {
        entities.Clear();
    }
}
