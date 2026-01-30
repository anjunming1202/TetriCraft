using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SceneLifecycle : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        OnEnter();
    }

    private void OnDestroy()
    {
        OnExit();
    }

    protected abstract void OnEnter();
    protected abstract void OnExit();
}
