// Block, the most basic unit, one block occupies one grid
using System;
using System.Collections;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

[Serializable]
public abstract class Block : MonoBehaviour
{
    private void Awake()
    {

    }

    // Identity
    public BlockID ID;

    // Data in the map
    private Vector2Int position = Vector2Int.zero; // Block position in the map
    public Vector2Int Position => position;

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isAnimating = false;    // is moving with animation

    // Events
    public delegate void OnChangedEvent();
    public event OnChangedEvent OnInstantPosChanged;
    public event OnChangedEvent OnMoved;
    public event OnChangedEvent OnLanded;

//  public event OnChangedEvent OnSpawned;
    public event OnChangedEvent OnDestroyed;



    // Position & Moving
    public Vector3 GetWorldPosition() => MapBoundaryData.GridToWorld(position);

    public void SetPosition(int x, int y, bool animation = false)
    {
        position = new Vector2Int(x, y);

        if (animation)
        {
            OnMoved?.Invoke();
        }
        else
        {
            transform.position = GetWorldPosition();
            OnInstantPosChanged?.Invoke();
        }
    }




    public void Destroy()
    {
        GameObject.Destroy(gameObject);
        OnDestroyed?.Invoke();
    }

    public void Lockdown()
    {
        isLocked = true;
        OnLanded?.Invoke();
    }



}