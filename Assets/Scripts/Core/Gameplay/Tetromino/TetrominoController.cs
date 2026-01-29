using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class TetrominoController : MonoBehaviour
{
    private MapTetromino tetromino;
    private MapManager map;

    // Active
    private bool isActive = false;

    // Control
    public float gravity => GameManager.preference.tetrominoDropSpeed / 10 + 0.5f;
    public float speedDrop = 1;
    public float speedSoftDrop = 2;
    public float keyInputInterval = 0.2f;

    private float dropTimer = 0;
    private bool isAccelerating = false;
    private float interval => isAccelerating ? intervalAccelerating : intervalNormal;
    private float intervalNormal => 1 / (gravity * speedDrop);
    private float intervalAccelerating => 1 / (gravity * speedSoftDrop);

    // Input actions
    private PlayerInput playerInput;
    private InputActionAsset inputActionAsset;
    private InputControls inputs; // generated C#

    private Coroutine leftCoroutine;
    private Coroutine rightCoroutine;
    private Coroutine rotateCCWCoroutine;
    private Coroutine rotateCWCoroutine;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inputs = new InputControls();

        inputs.Gameplay.Enable();

        inputs.Gameplay.LeftShift.started += ctx => OnStartRepeatingAction(ref leftCoroutine, OnLeft, inputs.Gameplay.LeftShift);
        inputs.Gameplay.LeftShift.canceled += ctx => OnCancelRepeatingAction(ref leftCoroutine);

        inputs.Gameplay.RightShift.started += ctx => OnStartRepeatingAction(ref rightCoroutine, OnRight, inputs.Gameplay.RightShift);
        inputs.Gameplay.RightShift.canceled += ctx => OnCancelRepeatingAction(ref rightCoroutine);

        inputs.Gameplay.LeftRotate.started += ctx => OnStartRepeatingAction(ref rotateCCWCoroutine, OnRotateCCW, inputs.Gameplay.LeftRotate);
        inputs.Gameplay.LeftRotate.canceled += ctx => OnCancelRepeatingAction(ref rotateCCWCoroutine);

        inputs.Gameplay.RightRotate.started += ctx => OnStartRepeatingAction(ref rotateCWCoroutine, OnRotateCW, inputs.Gameplay.RightRotate);
        inputs.Gameplay.RightRotate.canceled += ctx => OnCancelRepeatingAction(ref rotateCWCoroutine);

        inputs.Gameplay.SoftDrop.started += ctx => OnSoftDropStart();
        inputs.Gameplay.SoftDrop.canceled += ctx => OnSoftDropStop();

        inputs.Gameplay.HardDrop.performed += ctx => OnHardDrop();
    }

    void OnDestroy()
    {
        
    }

    public void Initialise(MapManager map, MapTetromino tetromino)
    {
        this.map = map;
        this.tetromino = tetromino;

        InitPlayerInput();

        Reset();
        Deactivate();
    }

    private void Reset()
    {
        isAccelerating = false;
        dropTimer = 0;
    }

    public void Activate()
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void OnUpdate()
    {
        if (!isActive)
            return;

        // Timer
        dropTimer += Time.deltaTime;

        // Drop of tetromino
        if (dropTimer >= interval)
        {
            if (isAccelerating)
                tetromino.SoftDrop(map); // Soft drop
            else
                tetromino.Drop(map); // Normal drop
            dropTimer = 0;
        }
    }

    // assign actions asset by player in the future
    private void InitPlayerInput()
    {
        if (InputRoot.Instance != null)
        {
            playerInput.actions = InputRoot.Instance.playerInput.actions;
            Debug.Log($"Actions set for player of {tetromino}!");
        }
    }

    private void OnLeft()
    {
        if (!isActive) return;
        tetromino.Left(map);
    }
    private void OnRight()
    {
        if (!isActive) return;
        tetromino.Right(map);
    }
    private void OnRotateCCW()
    {
        if (!isActive) return;
        tetromino.Rotate(map, false);
    }
    private void OnRotateCW()
    {
        if (!isActive) return;
        tetromino.Rotate(map, true);
    }
    private void OnSoftDropStart()
    {
        if (!isActive) return;
        dropTimer = 0;
        if (tetromino.TryImmediateLockdown(map)) // down key => skip delay and lockdown directly
            return;
        tetromino.SoftDrop(map);  // drop immediately
        isAccelerating = true;
    }
    /// <summary>
    /// Restore to natural Drop
    /// </summary>
    private void OnSoftDropStop()
    {
        isAccelerating = false;
    }
    private void OnHardDrop()
    {
        if (!isActive) return;
        dropTimer = 0;
        tetromino.HardDrop(map);
    }

    private IEnumerator RepeatAction(Action action, InputAction inputAction)
    {
        while (true)
        {
            yield return new WaitForSeconds(keyInputInterval);
            if (inputAction != null && !inputAction.IsPressed()) // extra check avoiding boundary race
            {
                //Debug.Log($"Breaking repeat aciton coroutine {action.ToString()}");
                yield break;
            }
            action();
        }
    }

    private void OnStartRepeatingAction(ref Coroutine coroutine, Action action, InputAction inputAction)
    {
        //Debug.Log("Coroutine started");
        action();
        if (coroutine == null)
            coroutine = StartCoroutine(RepeatAction(action, inputAction));
    }

    private void OnCancelRepeatingAction(ref Coroutine coroutine)
    {
        //Debug.Log("Coroutine cancelled");
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }
}
