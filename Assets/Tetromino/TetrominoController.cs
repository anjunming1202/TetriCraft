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

    float timer = 0;
    private bool isAccelerating = false;
    private float interval;
    private float intervalNormal => 1 / (gravity * speedDrop);
    private float intervalAccelerating => 1 / (gravity * speedSoftDrop);

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
        timer += Time.deltaTime;

        // Control
        if (Input.GetKeyDown(key_left)) // Left
        {
            tetromino.Left();
        }
        if (Input.GetKeyDown(key_right)) // Right
        {
            tetromino.Right();
        }
        if (Input.GetKeyDown(key_accelerate)) // Accelerating
        {
            timer = 0;
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
            timer = 0;
            tetromino.HardDrop();
        }
        if (Input.GetKeyDown(key_rotateCW)) // Rotate clockwise
        {
            tetromino.Rotate(true);
        }
        if (Input.GetKeyDown(key_rotateCCW)) // Rotate anticlockwise
        {
            tetromino.Rotate(false);
        }

        // Drop of tetromino
        if (timer >= interval)
        {
            if (isAccelerating)
                tetromino.SoftDrop(); // Soft drop
            else
                tetromino.Drop(); // Normal drop
            timer = 0;
        }
    }
}
