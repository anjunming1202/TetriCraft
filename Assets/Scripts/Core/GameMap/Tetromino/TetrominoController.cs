using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class TetrominoController : MonoBehaviour
{
    [SerializeField] public PlayerID PlayerID;
    [SerializeField] public string actionMapName;

    private MapTetromino tetromino;
    private MapManager map;

    // Active
    private bool isActive = false;

    // Control
    public float gravity => SettingsManager.Current[PlayerID].dropSpeed / 10 + 0.5f;
    public float speedDrop = 1;
    public float speedSoftDrop = 2;
    public float keyInputInterval = 0.2f;

    // Input action asset reference
    public InputActionAsset inputActionAsset => playerInput.actions;

    private float dropTimer = 0;
    private bool isAccelerating = false;
    private float interval => isAccelerating ? intervalAccelerating : intervalNormal;
    private float intervalNormal => 1 / (gravity * speedDrop);
    private float intervalAccelerating => 1 / (gravity * speedSoftDrop);

    // Input actions
    private PlayerInput playerInput;

    private InputAction left;
    private InputAction right;
    private InputAction rotateCCW;
    private InputAction rotateCW;
    private InputAction softDrop;
    private InputAction hardDrop;

    private Coroutine leftCoroutine;
    private Coroutine rightCoroutine;
    private Coroutine rotateCCWCoroutine;
    private Coroutine rotateCWCoroutine;

    private Action<InputAction.CallbackContext> _leftStartedHandler;
    private Action<InputAction.CallbackContext> _leftCanceledHandler;
    private Action<InputAction.CallbackContext> _rightStartedHandler;
    private Action<InputAction.CallbackContext> _rightCanceledHandler;
    private Action<InputAction.CallbackContext> _rotateCCWStartedHandler;
    private Action<InputAction.CallbackContext> _rotateCCWCanceledHandler;
    private Action<InputAction.CallbackContext> _rotateCWStartedHandler;
    private Action<InputAction.CallbackContext> _rotateCWCanceledHandler;
    private Action<InputAction.CallbackContext> _softDropStartedHandler;
    private Action<InputAction.CallbackContext> _softDropCanceledHandler;
    private Action<InputAction.CallbackContext> _hardDropPerformedHandler;

    /*private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        //playerInput.actions = InputRoot.Instance.playerInput.actions;
    }*/

    public void Initialise()
    {
        // player input reference
        playerInput = GetComponent<PlayerInput>();

        // input action references
        var actionMap = playerInput.actions.FindActionMap(actionMapName, throwIfNotFound: true);

        left = actionMap?.FindAction("Left");
        right = actionMap?.FindAction("Right");
        rotateCCW = actionMap?.FindAction("RotateCCW");
        rotateCW = actionMap?.FindAction("RotateCW");
        softDrop = actionMap?.FindAction("SoftDrop");
        hardDrop = actionMap?.FindAction("HardDrop");

        // controller action handlers
        _leftStartedHandler = ctx => OnStartRepeatingAction(ref leftCoroutine, OnLeft, left);
        _leftCanceledHandler = ctx => OnCancelRepeatingAction(ref leftCoroutine);
        _rightStartedHandler = ctx => OnStartRepeatingAction(ref rightCoroutine, OnRight, right);
        _rightCanceledHandler = ctx => OnCancelRepeatingAction(ref rightCoroutine);
        _rotateCCWStartedHandler = ctx => OnStartRepeatingAction(ref rotateCCWCoroutine, OnRotateCCW, rotateCCW);
        _rotateCCWCanceledHandler = ctx => OnCancelRepeatingAction(ref rotateCCWCoroutine);
        _rotateCWStartedHandler = ctx => OnStartRepeatingAction(ref rotateCWCoroutine, OnRotateCW, rotateCW);
        _rotateCWCanceledHandler = ctx => OnCancelRepeatingAction(ref rotateCWCoroutine);
        _softDropStartedHandler = ctx => OnSoftDropStart();
        _softDropCanceledHandler = ctx => OnSoftDropStop();
        _hardDropPerformedHandler = ctx => OnHardDrop();

        // binding input actions
        left.started += _leftStartedHandler;
        left.canceled += _leftCanceledHandler;

        right.started += _rightStartedHandler;
        right.canceled += _rightCanceledHandler;

        rotateCCW.started += _rotateCCWStartedHandler;
        rotateCCW.canceled += _rotateCCWCanceledHandler;

        rotateCW.started += _rotateCWStartedHandler;
        rotateCW.canceled += _rotateCWCanceledHandler;

        softDrop.started += _softDropStartedHandler;
        softDrop.canceled += _softDropCanceledHandler;

        hardDrop.performed += _hardDropPerformedHandler;
    }

    public void Dispose()
    {
        left.started -= _leftStartedHandler;
        left.canceled -= _leftCanceledHandler;

        right.started -= _rightStartedHandler;
        right.canceled -= _rightCanceledHandler;

        rotateCCW.started -= _rotateCCWStartedHandler;
        rotateCCW.canceled -= _rotateCCWCanceledHandler;

        rotateCW.started -= _rotateCWStartedHandler;
        rotateCW.canceled -= _rotateCWCanceledHandler;

        softDrop.started -= _softDropStartedHandler;
        softDrop.canceled -= _softDropCanceledHandler;

        hardDrop.performed -= _hardDropPerformedHandler;
    }

    public void Reset(MapManager map, MapTetromino tetromino)
    {
        this.map = map;
        this.tetromino = tetromino;

        InitPlayerInput();

        isAccelerating = false;
        dropTimer = 0;

        Deactivate();
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
