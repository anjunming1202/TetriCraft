using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    GameManager gameManager;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!gameManager.IsPaused)
                gameManager.PauseGame();
            else
                gameManager.ResumeGame();
        }
    }
}
