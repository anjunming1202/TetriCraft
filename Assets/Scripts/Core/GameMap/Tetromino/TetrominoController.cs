using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class TetrominoController : MonoBehaviour
{
    public PlayerID PlayerID;

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
    public static InputActionAsset inputActionAsset => playerInput.actions;

    private float dropTimer = 0;
    private bool isAccelerating = false;
    private float interval => isAccelerating ? intervalAccelerating : intervalNormal;
    private float intervalNormal => 1 / (gravity * speedDrop);
    private float intervalAccelerating => 1 / (gravity * speedSoftDrop);

    // Input actions
    private static PlayerInput playerInput;

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

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void Start()
    {
        var actionMap = playerInput.actions.FindActionMap("Gameplay");

        left = actionMap?.FindAction("Left");
        right = actionMap?.FindAction("Right");
        rotateCCW = actionMap?.FindAction("RotateCCW");
        rotateCW = actionMap?.FindAction("RotateCW");
        softDrop = actionMap?.FindAction("SoftDrop");
        hardDrop = actionMap?.FindAction("HardDrop");

        left.started += ctx => OnStartRepeatingAction(ref leftCoroutine, OnLeft, left);
        left.canceled += ctx => OnCancelRepeatingAction(ref leftCoroutine);

        right.started += ctx => OnStartRepeatingAction(ref rightCoroutine, OnRight, right);
        right.canceled += ctx => OnCancelRepeatingAction(ref rightCoroutine);

        rotateCCW.started += ctx => OnStartRepeatingAction(ref rotateCCWCoroutine, OnRotateCCW, rotateCCW);
        rotateCCW.canceled += ctx => OnCancelRepeatingAction(ref rotateCCWCoroutine);

        rotateCW.started += ctx => OnStartRepeatingAction(ref rotateCWCoroutine, OnRotateCW, rotateCW);
        rotateCW.canceled += ctx => OnCancelRepeatingAction(ref rotateCWCoroutine);

        softDrop.started += ctx => OnSoftDropStart();
        softDrop.canceled += ctx => OnSoftDropStop();

        hardDrop.performed += ctx => OnHardDrop();
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
