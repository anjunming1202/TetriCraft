using UnityEngine;

public class TetrominoController : MonoBehaviour
{
    private TetrominoManager tetromino;
    private MapManager mapManager;

    // Control
    float timer = 0;
    private bool isAccelerating = false;
    private float interval;
    private float intervalNormal => 1 / GameManager.SpeedDrop;
    private float intervalAccelerating => 1 / GameManager.SpeedSoftDrop;

    // Key
    private KeyCode key_left = KeyCode.A;
    private KeyCode key_right = KeyCode.D;
    private KeyCode key_accelerate = KeyCode.S;
    private KeyCode key_land = KeyCode.Space;
    private KeyCode key_rotateCW = KeyCode.E;
    private KeyCode key_rotateCCW = KeyCode.Q;




    private void Update()
    {

    }

    public void Initialise(TetrominoManager tetromino, MapManager mapManager)
    {
        this.tetromino = tetromino;
        this.mapManager = mapManager;
    }

}
