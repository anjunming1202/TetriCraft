using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSoundManager : MonoBehaviour
{
    public AudioClip clearLine;
    public AudioClip clearLineFour;

    private void Awake()
    {
        mapManager = GetComponent<TetrisManager>();

        mapManager.OnLineClear += PlayLineClearSound;
    }

    private void PlayLineClearSound(TetrisManager mapManager)
    {
        if (mapManager.lastClearLineCount < 4)
            AudioManager.Instance.PlaySound(clearLine);
        else
            AudioManager.Instance.PlaySound(clearLineFour);
    }

    private TetrisManager mapManager;
}
