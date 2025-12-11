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
        switch (scaleMode)
        {
            case ScaleMode.ConstantWidthRatio:
                _camera.orthographicSize = target.localScale.x / scale / 2 / _camera.aspect;
                break;
            case ScaleMode.ConstantHeightRatio:
                _camera.orthographicSize = target.localScale.y / scale / 2;
                break;
            case ScaleMode.MaximumFit:
                float widthControlSize = target.localScale.x / scale / 2 / _camera.aspect;
                float heightControlSize = target.localScale.y / scale / 2;
                _camera.orthographicSize = Mathf.Max(widthControlSize, heightControlSize);
                break;
        }
    }
}
