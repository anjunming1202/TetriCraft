using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [Serializable]
    enum ScaleMode
    {
        ConstantWidthRatio,
        ConstantHeightRatio,
        MaximumFit
    }

    [SerializeField] private Transform target;
    [SerializeField] private ScaleMode scaleMode;
    [SerializeField] private float scale;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (GameStateMachine.State == GameStateType.Playing)
        {
            _camera.orthographicSize = GetTargetSize();
        }
    }

    public float GetTargetSize()
    {
        switch (scaleMode)
        {
            case ScaleMode.ConstantWidthRatio:
                return target.localScale.x / scale / 2 / _camera.aspect;
            case ScaleMode.ConstantHeightRatio:
                return target.localScale.y / scale / 2;
            case ScaleMode.MaximumFit:
                float widthControlSize = target.localScale.x / scale / 2 / _camera.aspect;
                float heightControlSize = target.localScale.y / scale / 2;
                return Mathf.Max(widthControlSize, heightControlSize);
        }
        return -1;
    }
}
