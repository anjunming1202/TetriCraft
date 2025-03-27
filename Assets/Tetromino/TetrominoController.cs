using UnityEngine;

public class TetrominoController : MonoBehaviour
{
    private MapTetromino tetromino;

    // Active
    public bool isActive = false;

    // Control
    public float gravity = 1;
    public float speedDrop = 1;
    public float speedSoftDrop = 2;
    public float keyInputInterval = 0.2f;

    private float dropTimer = 0;
    private bool isAccelerating = false;
    private float interval;
    private float intervalNormal => 1 / (gravity * speedDrop);
    private float intervalAccelerating => 1 / (gravity * speedSoftDrop);

    private float keyInputTimer = 0;

    // Key
    private KeyCode key_left = KeyCode.A;
    private KeyCode key_right = KeyCode.D;
    private KeyCode key_accelerate = KeyCode.S;
    private KeyCode key_land = KeyCode.Space;
    private KeyCode key_rotateCW = KeyCode.E;
    private KeyCode key_rotateCCW = KeyCode.Q;



    private void Awake()
    {
        tetromino = GetComponent<MapTetromino>();
        interval = intervalNormal;
    }
    private void Update()
    {
        // Timer
        dropTimer += Time.deltaTime;

        // Control
        if (Input.anyKeyDown)
        {
            keyInputTimer = keyInputInterval;
        }
        if (Input.anyKey)
        {
            keyInputTimer += Time.deltaTime;
        }
        if (Input.GetKey(key_left)) // Left
        {
            if (keyInputTimer > keyInputInterval)
            { 
                tetromino.Left();
            }
        }
        if (Input.GetKey(key_right)) // Right
        {
            if (keyInputTimer > keyInputInterval)
            {
                tetromino.Right();
            }
        }
        if (Input.GetKeyDown(key_accelerate)) // Accelerating
        {
            dropTimer = 0;
            if (tetromino.TryImmediateLockdown()) // down key => skip delay and lockdown directly
                return;
            tetromino.SoftDrop();  // drop immediately
            isAccelerating = true;
            interval = intervalAccelerating;
        }
        if (Input.GetKeyUp(key_accelerate))
        {
            isAccelerating = false;
            interval = intervalNormal;
        }
        if (Input.GetKeyDown(key_land)) // Hard drop
        {
            dropTimer = 0;
            tetromino.HardDrop();
        }
        if (Input.GetKey(key_rotateCW)) // Rotate clockwise
        {
            if (keyInputTimer > keyInputInterval)
            {
                tetromino.Rotate(true);
            }
        }
        if (Input.GetKey(key_rotateCCW)) // Rotate anticlockwise
        {
            if (keyInputTimer > keyInputInterval)
            {
                tetromino.Rotate(false);
            }
        }
        if (Input.anyKey)
        {   
            if (keyInputTimer > keyInputInterval)
            {
                keyInputTimer = 0;
            }
        }

        // Drop of tetromino
        if (dropTimer >= interval)
        {
            if (isAccelerating)
                tetromino.SoftDrop(); // Soft drop
            else
                tetromino.Drop(); // Normal drop
            dropTimer = 0;
        }
    }
}
