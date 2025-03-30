using UnityEngine;

public class TetrominoController : MonoBehaviour
{
    private MapTetromino tetromino;
    private Map map;

    // Active
    private bool isActive = false;

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



    public void Initialise(Map map, MapTetromino tetromino)
    {
        this.map = map;
        this.tetromino = tetromino;
        interval = intervalNormal;
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

    private void Update()
    {
        if (!isActive)
            return;

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
                tetromino.Left(map);
            }
        }
        if (Input.GetKey(key_right)) // Right
        {
            if (keyInputTimer > keyInputInterval)
            {
                tetromino.Right(map);
            }
        }
        if (Input.GetKeyDown(key_accelerate)) // Accelerating
        {
            dropTimer = 0;
            if (tetromino.TryImmediateLockdown(map)) // down key => skip delay and lockdown directly
                return;
            tetromino.SoftDrop(map);  // drop immediately
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
            tetromino.HardDrop(map);
        }
        if (Input.GetKey(key_rotateCW)) // Rotate clockwise
        {
            if (keyInputTimer > keyInputInterval)
            {
                tetromino.Rotate(map, true);
            }
        }
        if (Input.GetKey(key_rotateCCW)) // Rotate anticlockwise
        {
            if (keyInputTimer > keyInputInterval)
            {
                tetromino.Rotate(map, false);
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
                tetromino.SoftDrop(map); // Soft drop
            else
                tetromino.Drop(map); // Normal drop
            dropTimer = 0;
        }
    }
}
